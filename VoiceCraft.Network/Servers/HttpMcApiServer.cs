using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using LiteNetLib.Utils;
using VoiceCraft.Core.JsonConverters;
using VoiceCraft.Core.World;
using VoiceCraft.Network.NetPeers;
using VoiceCraft.Network.Packets.McApiPackets;
using VoiceCraft.Network.Packets.McApiPackets.Request;
using VoiceCraft.Network.Packets.McApiPackets.Response;
using VoiceCraft.Network.Systems;

namespace VoiceCraft.Network.Servers;

public class HttpMcApiServer(VoiceCraftWorld world, AudioEffectSystem audioEffectSystem)
    : McApiServer(world, audioEffectSystem)
{
    private const int MaxRequestLength = 1_000_000;

    private HttpMcApiConfig _config = new();
    private readonly ConcurrentDictionary<string, HttpMcApiNetPeer> _mcApiPeers = new();
    private readonly NetDataReader _httpReader = new();
    private readonly NetDataWriter _httpWriter = new();
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
                $"McHttp cannot bind to '{_config.Hostname}' due to insufficient permissions for this address/port.",
                ex);
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
        if (_httpServer == null)
        {
            _mcApiPeers.Clear();
            return;
        }

        try
        {
            if (_httpServer.IsListening)
                _httpServer.Stop();
        }
        catch
        {
            //Do Nothing
        }

        try
        {
            _httpServer.Close();
        }
        catch
        {
            //Do Nothing
        }

        _httpServer = null;
        _mcApiPeers.Clear();
    }

    public override void SendPacket<T>(McApiNetPeer netPeer, T packet)
    {
        if (_httpServer == null || 
            Config.DisabledPacketTypes.Contains(packet.PacketType) ||
            netPeer.ConnectionState == McApiConnectionState.Disconnected ||
            (packet is IMcApiEventPacket eventPacket && 
            !netPeer.SubscribedEvents.Contains(eventPacket.EventType))) return;
        try
        {
            lock (_writer)
            {
                _writer.Reset();
                _writer.Put((byte)packet.PacketType);
                _writer.Put(packet);
                if (_writer.Length > short.MaxValue)
                    throw new ArgumentOutOfRangeException(nameof(packet));

                netPeer.OutgoingQueue.Enqueue(new McApiNetPeer.QueuedPacket(_writer.CopyData(), string.Empty));
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
            byte[] data;
            lock (_writer)
            {
                _writer.Reset();
                _writer.Put((byte)packet.PacketType);
                _writer.Put(packet);
                if (_writer.Length > short.MaxValue)
                    throw new ArgumentOutOfRangeException(nameof(packet));
                data = _writer.CopyData();
            }

            foreach (var netPeer in _mcApiPeers.Values)
            {
                //Broadcast to only connected and subscribed clients.
                if (netPeer.ConnectionState != McApiConnectionState.Connected || 
                    excludes.Contains(netPeer) ||
                    (packet is IMcApiEventPacket eventPacket && 
                     !netPeer.SubscribedEvents.Contains(eventPacket.EventType)))
                    continue;
                netPeer.OutgoingQueue.Enqueue(new McApiNetPeer.QueuedPacket(data, string.Empty));
            }
        }
        finally
        {
            PacketPool<T>.Return(packet);
        }
    }

    public override void Disconnect(McApiNetPeer netPeer, bool force = false)
    {
        if (netPeer is not HttpMcApiNetPeer httpNetPeer) return;
        if (netPeer.ConnectionState is McApiConnectionState.Disconnected or McApiConnectionState.Disconnecting) return;
        var wasConnected = httpNetPeer.ConnectionState == McApiConnectionState.Connected;
        httpNetPeer.ConnectionState = McApiConnectionState.Disconnecting;
        var sessionToken = httpNetPeer.SessionToken;
        try
        {
            if (force)
            {
                _mcApiPeers.TryRemove(httpNetPeer.SessionToken, out _); //Remove Immediately.
                return;
            }

            SendPacket(netPeer, PacketPool<McApiLogoutRequestPacket>.GetPacket(() => new McApiLogoutRequestPacket())
                .Set(netPeer.SessionToken));
        }
        finally
        {
            httpNetPeer.SetSessionToken(string.Empty);
            httpNetPeer.ConnectionState = McApiConnectionState.Disconnected;
            if (wasConnected)
                OnPeerDisconnected?.Invoke(httpNetPeer, sessionToken);
        }
    }

    protected override void AcceptRequest(McApiLoginRequestPacket packet, McApiNetPeer netPeer)
    {
        if (netPeer is not HttpMcApiNetPeer httpNetPeer) return;
        try
        {
            if (httpNetPeer.ConnectionState != McApiConnectionState.Connected)
                httpNetPeer.SetSessionToken(Guid.NewGuid().ToString());

            SendPacket(httpNetPeer,
                PacketPool<McApiAcceptResponsePacket>.GetPacket(() => new McApiAcceptResponsePacket())
                    .Set(packet.RequestId, httpNetPeer.SessionToken));

            httpNetPeer.ConnectionState = McApiConnectionState.Connected;
            OnPeerConnected?.Invoke(httpNetPeer, httpNetPeer.SessionToken);
        }
        catch
        {
            RejectRequest(packet, "McApi.DisconnectReason.Error", httpNetPeer); //Auth flow is a bit different here.
        }
    }

    protected override void RejectRequest(McApiLoginRequestPacket packet, string reason, McApiNetPeer netPeer)
    {
        if (netPeer is not HttpMcApiNetPeer httpNetPeer) return;
        try
        {
            SendPacket(httpNetPeer, PacketPool<McApiDenyResponsePacket>.GetPacket(() => new McApiDenyResponsePacket())
                .Set(packet.RequestId, reason));
        }
        finally
        {
            httpNetPeer.SetSessionToken(string.Empty);
            httpNetPeer.ConnectionState = McApiConnectionState.Disconnected;
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
                _ = Task.Run(async () => await HandleRequest(context)); //Threadpool it.
            }
        }
        catch
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

            switch (context.Request.ContentLength64)
            {
                case < 0:
                    context.Response.StatusCode = 411;
                    context.Response.Close();
                    return;
                //Do not accept anything higher than a mb.
                case > MaxRequestLength:
                    context.Response.StatusCode = 413;
                    context.Response.Close();
                    return;
            }

            var size = (int)context.Request.ContentLength64;
            var data = ArrayPool<byte>.Shared.Rent(size);
            var packets = new List<byte[]>();
            try
            {
                await context.Request.InputStream.ReadExactlyAsync(data, 0, size);
                var stringData = Encoding.UTF8.GetString(data.AsSpan(0, size));
                if (!TryReadPackedPackets(stringData, packets))
                {
                    context.Response.StatusCode = 400;
                    context.Response.Close();
                    return;
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(data);
            }

            var token = context.Request.Headers.Get("Authorization")?.Remove(0, 7);
            HttpMcApiNetPeer? netPeer;
            if (context.Request.Url?.AbsolutePath.StartsWith("/connect") ?? false)
            {
                netPeer = HandleConnectRequest(packets);
            }
            else if (string.IsNullOrWhiteSpace(token) || !_mcApiPeers.TryGetValue(token, out netPeer))
            {
                context.Response.StatusCode = 401;
                context.Response.Close();
                return;
            }
            else
            {
                ReceivePacketsLogic(netPeer, packets, token);
            }

            SendPacketsLogic(context, netPeer, packets);
        }
        catch
        {
            try
            {
                context.Response.StatusCode = 500;
                context.Response.Close();
            }
            catch
            {
                //Do Nothing
            }
        }
    }

    private HttpMcApiNetPeer HandleConnectRequest(List<byte[]> packets)
    {
        var netPeer = new HttpMcApiNetPeer
        {
            Tag = this,
            ConnectionState = McApiConnectionState.Connecting
        };
        var tempToken = Guid.NewGuid().ToString();
        if (!_mcApiPeers.TryAdd(tempToken, netPeer))
        {
            throw new Exception(); //Failure
        }

        ReceivePacketsLogic(netPeer, packets, netPeer.SessionToken);
        var sw = new SpinWait();
        while (netPeer.ConnectionState == McApiConnectionState.Connecting)
        {
            sw.SpinOnce();
        }

        if (netPeer.ConnectionState != McApiConnectionState.Connected)
        {
            _mcApiPeers.TryRemove(tempToken, out _); //Means it failed, we remove immediately.
        }
        //Swap to new session token.
        else if (!_mcApiPeers.TryRemove(tempToken, out _) || !_mcApiPeers.TryAdd(netPeer.SessionToken, netPeer))
        {
            throw new Exception(); //Failure
        }

        return netPeer;
    }

    private static void ReceivePacketsLogic(HttpMcApiNetPeer httpNetPeer, IEnumerable<byte[]> packets, string token)
    {
        foreach (var data in packets)
        {
            if (data.Length == 0) continue;
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

    private void SendPacketsLogic(HttpListenerContext context, HttpMcApiNetPeer netPeer, List<byte[]> packets)
    {
        while (netPeer.OutgoingQueue.TryDequeue(out var packet))
        {
            try
            {
                packets.Add(packet.Data);
            }
            catch
            {
                //Do Nothing
            }
        }

        string encoded;
        lock (_httpWriter)
        {
            _httpWriter.Reset();
            foreach (var packet in packets)
            {
                _httpWriter.Put((ushort)packet.Length);
                _httpWriter.Put(packet);
            }

            encoded = Z85.GetStringWithPadding(_httpWriter.AsReadOnlySpan());
        }

        var bufferSize = Encoding.UTF8.GetMaxByteCount(encoded.Length); //Zero alloc. GetByteCount allocates a char[].
        var buffer = ArrayPool<byte>.Shared.Rent(bufferSize); //Use Pooling.
        try
        {
            var encodedBytes = Encoding.UTF8.GetBytes(encoded, buffer);
            context.Response.StatusCode = 200;
            context.Response.ContentLength64 = encodedBytes;
            context.Response.OutputStream.Write(buffer.AsSpan(0, encodedBytes));
            context.Response.OutputStream.Close();
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private bool TryReadPackedPackets(string stringData, List<byte[]> packets)
    {
        try
        {
            var packedPackets = Z85.GetBytesWithPadding(stringData);
            lock (_httpReader)
            {
                _httpReader.Clear();
                _httpReader.SetSource(packedPackets);
                while (!_httpReader.EndOfData)
                {
                    var packetSize = _httpReader.GetUShort();
                    if (packetSize <= 0) continue;
                    if (_httpReader.AvailableBytes < packetSize)
                        return false;

                    var packet = new byte[packetSize];
                    _httpReader.GetBytes(packet, packetSize);
                    packets.Add(packet);
                }
            }

            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    private void UpdatePeer(string lookupToken, HttpMcApiNetPeer httpNetPeer)
    {
        ProcessPackets(httpNetPeer);
        if (DateTime.UtcNow - httpNetPeer.LastUpdate < TimeSpan.FromMilliseconds(Config.MaxTimeoutMs)) return;
        Disconnect(httpNetPeer);
        //Double the amount of time. We remove the peer.
        if (DateTime.UtcNow - httpNetPeer.LastUpdate < TimeSpan.FromMilliseconds(Config.MaxTimeoutMs * 2)) return;
        _mcApiPeers.TryRemove(lookupToken, out _);
    }

    private void ProcessPackets(HttpMcApiNetPeer httpNetPeer)
    {
        lock (_reader)
        {
            while (httpNetPeer.IncomingQueue.TryDequeue(out var packet))
                try
                {
                    var packetToken = packet.Token;
                    _reader.Clear();
                    _reader.SetSource(packet.Data);
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