using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using LiteNetLib.Utils;
using VoiceCraft.Core.JsonConverters;
using VoiceCraft.Core.World;
using VoiceCraft.Network.NetPeers;
using VoiceCraft.Network.Packets.McApiPackets.Request;
using VoiceCraft.Network.Packets.McApiPackets.Response;
using VoiceCraft.Network.Systems;

namespace VoiceCraft.Network.Servers;

public class HttpMcApiServer(VoiceCraftWorld world, AudioEffectSystem audioEffectSystem)
    : McApiServer(world, audioEffectSystem)
{
    private HttpMcApiConfig _config = new();
    private readonly ConcurrentDictionary<IPAddress, HttpMcApiNetPeer> _mcApiPeers = new();
    private readonly NetDataReader _reader = new();
    private readonly NetDataWriter _writer = new();
    private HttpListener? _httpServer;

    public HttpMcApiConfig Config
    {
        get => _config;
        set
        {
            if (_httpServer != null)
                throw new InvalidOperationException();
            _config = value;
        }
    }

    public override uint MaxClients => Config.MaxClients;
    public override string LoginToken => Config.LoginToken;

    public override int ConnectedPeers =>
        _mcApiPeers.Count(x => x.Value.ConnectionState == McApiConnectionState.Connected);

    public override event Action<McApiNetPeer, string>? OnPeerConnected;
    public override event Action<McApiNetPeer, string>? OnPeerDisconnected;

    public override void Start()
    {
        Stop();
        var listenerPrefix = BuildListenerPrefix(_config.Hostname);
        _httpServer = new HttpListener();
        _httpServer.Prefixes.Add(listenerPrefix);
        try
        {
            _httpServer.Start();
        }
        catch (HttpListenerException ex) when (ex.NativeErrorCode == 99)
        {
            throw new InvalidOperationException(
                $"McHttp cannot bind to '{_config.Hostname}'. This address is not available inside the current environment/container. " +
                "Use 0.0.0.0 or the container local IP from 'ip a'.", ex);
        }
        catch (HttpListenerException ex) when (ex.NativeErrorCode == 13)
        {
            throw new InvalidOperationException(
                $"McHttp cannot bind to '{_config.Hostname}' due to insufficient permissions for this address/port.", ex);
        }

        _ = ListenerLoop(_httpServer);
    }

    public override void Update()
    {
        if (_httpServer == null) return;
        foreach (var peer in _mcApiPeers) UpdatePeer(peer.Key, peer.Value);
    }

    public override void Stop()
    {
        if (_httpServer == null) return;
        try
        {
            if (_httpServer.IsListening)
                _httpServer.Stop();
        }
        catch (ObjectDisposedException)
        {
            //Do Nothing
        }
        catch (HttpListenerException)
        {
            //Do Nothing
        }

        try
        {
            _httpServer.Close();
        }
        catch (ObjectDisposedException)
        {
            //Do Nothing
        }

        _httpServer = null;
    }

    public override void SendPacket<T>(McApiNetPeer netPeer, T packet)
    {
        if (_httpServer == null || netPeer.ConnectionState != McApiConnectionState.Connected ||
            Config.DisabledPacketTypes.Contains(packet.PacketType)) return;
        try
        {
            lock (_writer)
            {
                _writer.Reset();
                _writer.Put((byte)packet.PacketType);
                _writer.Put(packet);
                if (_writer.Length > short.MaxValue)
                    throw new ArgumentOutOfRangeException(nameof(packet));

                var encodedPacket = McApiStringCodec.Encode(_writer.AsReadOnlySpan());
                netPeer.OutgoingQueue.Enqueue(new McApiNetPeer.QueuedPacket(encodedPacket, string.Empty));
            }
        }
        finally
        {
            PacketPool<T>.Return(packet);
        }
    }

    public override void Broadcast<T>(T packet, params McApiNetPeer?[] excludes)
    {
        if (_httpServer == null || Config.DisabledPacketTypes.Contains(packet.PacketType)) return;
        try
        {
            lock (_writer)
            {
                var netPeers = _mcApiPeers.Where(x => x.Value.ConnectionState == McApiConnectionState.Connected);
                _writer.Reset();
                _writer.Put((byte)packet.PacketType);
                _writer.Put(packet);
                if (_writer.Length > short.MaxValue)
                    throw new ArgumentOutOfRangeException(nameof(packet));

                var encodedPacket = McApiStringCodec.Encode(_writer.AsReadOnlySpan());
                foreach (var netPeer in netPeers)
                {
                    if (excludes.Contains(netPeer.Value)) continue;
                    netPeer.Value.OutgoingQueue.Enqueue(new McApiNetPeer.QueuedPacket(encodedPacket, string.Empty));
                }
            }
        }
        finally
        {
            PacketPool<T>.Return(packet);
        }
    }

    public override void Disconnect(McApiNetPeer netPeer, bool force = false)
    {
        if (netPeer is not HttpMcApiNetPeer { ConnectionState: McApiConnectionState.Connected } httpNetPeer) return;
        var logoutPacket = PacketPool<McApiLogoutRequestPacket>.GetPacket(() => new McApiLogoutRequestPacket())
            .Set(netPeer.SessionToken);
        try
        {
            var sessionToken = httpNetPeer.SessionToken;
            httpNetPeer.SetConnectionState(McApiConnectionState.Disconnected);
            httpNetPeer.SetSessionToken(string.Empty);
            OnPeerDisconnected?.Invoke(httpNetPeer, sessionToken);
            if (force)
            {
                _mcApiPeers.TryRemove(httpNetPeer.IpAddress, out _); //Remove Immediately.
                return;
            }

            lock (_writer)
            {
                _writer.Reset();
                _writer.Put((byte)logoutPacket.PacketType);
                _writer.Put(logoutPacket);
                if (_writer.Length > short.MaxValue)
                    throw new ArgumentOutOfRangeException(nameof(netPeer));

                var encodedPacket = McApiStringCodec.Encode(_writer.AsReadOnlySpan());
                netPeer.OutgoingQueue.Enqueue(new McApiNetPeer.QueuedPacket(encodedPacket, string.Empty));
            }
        }
        finally
        {
            PacketPool<McApiLogoutRequestPacket>.Return(logoutPacket);
        }
    }

    protected override void AcceptRequest(McApiLoginRequestPacket packet, object? data)
    {
        if (data is not HttpMcApiNetPeer httpNetPeer) return;
        try
        {
            if (httpNetPeer.ConnectionState != McApiConnectionState.Connected)
            {
                httpNetPeer.SetSessionToken(Guid.NewGuid().ToString());
                httpNetPeer.SetConnectionState(McApiConnectionState.Connected);
            }

            SendPacket(httpNetPeer,
                PacketPool<McApiAcceptResponsePacket>.GetPacket(() => new McApiAcceptResponsePacket())
                    .Set(packet.RequestId, httpNetPeer.SessionToken));
            OnPeerConnected?.Invoke(httpNetPeer, httpNetPeer.SessionToken);
        }
        catch
        {
            RejectRequest(packet, "McApi.DisconnectReason.Error", httpNetPeer); //Auth flow is a bit different here.
        }
    }

    protected override void RejectRequest(McApiLoginRequestPacket packet, string reason, object? data)
    {
        if (data is not HttpMcApiNetPeer httpNetPeer) return;
        var responsePacket = PacketPool<McApiDenyResponsePacket>.GetPacket(() => new McApiDenyResponsePacket())
            .Set(packet.RequestId, reason);
        try
        {
            httpNetPeer.SetSessionToken("");
            httpNetPeer.SetConnectionState(McApiConnectionState.Disconnected);
            lock (_writer)
            {
                _writer.Reset();
                _writer.Put((byte)responsePacket.PacketType);
                _writer.Put(responsePacket);
                if (_writer.Length > short.MaxValue)
                    throw new ArgumentOutOfRangeException(nameof(packet));

                var encodedPacket = McApiStringCodec.Encode(_writer.AsReadOnlySpan());
                httpNetPeer.OutgoingQueue.Enqueue(new McApiNetPeer.QueuedPacket(encodedPacket, string.Empty));
            }
        }
        finally
        {
            PacketPool<McApiDenyResponsePacket>.Return(responsePacket);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (Disposed) return;
        base.Dispose(disposing);
        if (!disposing) return;
        OnPeerConnected = null;
        OnPeerDisconnected = null;
    }

    private async Task ListenerLoop(HttpListener listener)
    {
        try
        {
            while (listener.IsListening)
            {
                var context = await listener.GetContextAsync();
                await HandleRequest(context);
            }
        }
        catch (ObjectDisposedException)
        {
            //Do Nothing
        }
        catch (HttpListenerException)
        {
            //Do Nothing
        }
    }

    private async Task HandleRequest(HttpListenerContext context)
    {
        try
        {
            if (context.Request.HttpMethod != "POST")
            {
                context.Response.StatusCode = 403;
                context.Response.Close();
                return;
            }

            if (context.Request.ContentLength64 >= 1e+6) //Do not accept anything higher than a mb.
            {
                context.Response.StatusCode = 413;
                context.Response.Close();
                return;
            }

            var token = context.Request.Headers.Get("Authorization")?.Remove(0, 7);
            if (string.IsNullOrWhiteSpace(token))
            {
                context.Response.StatusCode = 401;
                context.Response.Close();
                return;
            }
            
            var size = (int)context.Request.InputStream.Length;
            var data = ArrayPool<byte>.Shared.Rent(size);
            List<string> packets;
            try
            {
                var stringData = Encoding.UTF8.GetString(data);
                packets = McWssPacketFraming.Unpack(stringData);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(data);
            }
            
            var netPeer = GetOrCreatePeer(context.Request.RemoteEndPoint.Address);
            ReceivePacketsLogic(netPeer, packets, token);
            packets.Clear();
            SendPacketsLogic(netPeer, packets);
            
            var buffer = Encoding.UTF8.GetBytes(McWssPacketFraming.Pack(packets));
            context.Response.StatusCode = 200;
            context.Response.ContentLength64 = buffer.Length;
            await context.Response.OutputStream.WriteAsync(buffer);
            context.Response.OutputStream.Close();
        }
        catch (JsonException)
        {
            context.Response.StatusCode = 400;
            context.Response.Close();
        }
        catch
        {
            context.Response.StatusCode = 500;
            context.Response.Close();
        }
    }

    private HttpMcApiNetPeer GetOrCreatePeer(IPAddress ipAddress)
    {
        return _mcApiPeers.GetOrAdd(ipAddress, _ =>
        {
            var httpNetPeer = new HttpMcApiNetPeer(ipAddress)
            {
                Tag = this
            };
            return httpNetPeer;
        });
    }

    private static void ReceivePacketsLogic(HttpMcApiNetPeer httpNetPeer, List<string> packets, string token)
    {
        foreach (var data in packets.Where(data => data.Length > 0))
        {
            try
            {
                httpNetPeer.IncomingQueue.Enqueue(new McApiNetPeer.QueuedPacket(data, token));
            }
            catch
            {
                //Do Nothing
            }
        }
    }

    private static void SendPacketsLogic(HttpMcApiNetPeer netPeer, List<string> packets)
    {
        while (netPeer.OutgoingQueue.TryDequeue(out var packet))
        {
            try
            {
                packets.Add(packet.StringData);
            }
            catch
            {
                //Do Nothing
            }
        }
    }

    private void UpdatePeer(IPAddress ipAddress, HttpMcApiNetPeer httpNetPeer)
    {
        lock (_reader)
        {
            while (httpNetPeer.IncomingQueue.TryDequeue(out var packet))
                try
                {
                    var packetToken = packet.Token;
                    _reader.Clear();
                    _reader.SetSource(McApiStringCodec.Decode(packet.StringData));
                    ProcessPacket(_reader, mcApiPacket =>
                    {
                        httpNetPeer.LastUpdate = DateTime.UtcNow;
                        if (!AuthorizePacket(mcApiPacket, httpNetPeer, packetToken) ||
                            Config.DisabledPacketTypes.Contains(mcApiPacket.PacketType)) return;
                        ExecutePacket(mcApiPacket, httpNetPeer);
                    });
                }
                catch
                {
                    //Do Nothing
                }
        }
        
        if (DateTime.UtcNow - httpNetPeer.LastUpdate < TimeSpan.FromMilliseconds(Config.MaxTimeoutMs)) return;
        Disconnect(httpNetPeer);
        //Double the amount of time. We remove the peer.
        if (DateTime.UtcNow - httpNetPeer.LastUpdate < TimeSpan.FromMilliseconds(Config.MaxTimeoutMs * 2)) return;
        _mcApiPeers.TryRemove(ipAddress, out _);
    }

    private static string BuildListenerPrefix(string configuredHostname)
    {
        if (!Uri.TryCreate(configuredHostname, UriKind.Absolute, out var uri))
            throw new InvalidOperationException(
                $"Invalid McHttp hostname '{configuredHostname}'. Expected format like 'http://0.0.0.0:9050/'.");
        if (!string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException(
                $"Invalid McHttp hostname '{configuredHostname}'. Only 'http://' is supported.");

        var port = uri.IsDefaultPort ? 80 : uri.Port;
        if (port is < 1 or > 65535)
            throw new InvalidOperationException($"Invalid McHttp port '{port}' in hostname '{configuredHostname}'.");

        var host = uri.Host;
        if (host == "0.0.0.0")
        {
            var replaced = $"{Uri.UriSchemeHttp}://+:{port}/";
            Console.WriteLine(
                $"[McHttp] Hostname '{configuredHostname}' uses 0.0.0.0. Replacing listener host with '+' => '{replaced}'.");
            host = "+";
        }

        return $"{Uri.UriSchemeHttp}://{host}:{port}/";
    }

    public class HttpMcApiConfig
    {
        [JsonConverter(typeof(JsonBooleanConverter))]
        public bool Enabled { get; set; } = true;

        public string LoginToken { get; set; } = Guid.NewGuid().ToString();
        public string Hostname { get; set; } = "http://127.0.0.1:9050/";
        public uint MaxClients { get; set; } = 1;
        public uint MaxTimeoutMs { get; set; } = 10000;
        public HashSet<McApiPacketType> DisabledPacketTypes { get; set; } = [];
    }
}
