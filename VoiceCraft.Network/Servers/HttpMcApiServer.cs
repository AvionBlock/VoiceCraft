using System;
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
using VoiceCraft.Network.Packets.McHttpPackets;
using VoiceCraft.Network.Systems;

namespace VoiceCraft.Network.Servers;

public class HttpMcApiServer(VoiceCraftWorld world, AudioEffectSystem audioEffectSystem)
    : McApiServer(world, audioEffectSystem)
{
    public override event Action<McApiNetPeer, string>? OnPeerConnected;
    public override event Action<McApiNetPeer, string>? OnPeerDisconnected;

    private HttpMcApiConfig _config = new();
    private readonly ConcurrentDictionary<IPEndPoint, HttpMcApiNetPeer> _mcApiPeers = new();
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

    public override void Start()
    {
        Stop();
        _httpServer = new HttpListener();
        _httpServer.Prefixes.Add(_config.Hostname);
        _httpServer.Start();
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
        _httpServer.Stop();
        _httpServer.Close();
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

                var encodedPacket = Z85.GetStringWithPadding(_writer.AsReadOnlySpan());
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

                var encodedPacket = Z85.GetStringWithPadding(_writer.AsReadOnlySpan());
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
        if (netPeer is not HttpMcApiNetPeer httpNetPeer) return;
        var logoutPacket = PacketPool<McApiLogoutRequestPacket>.GetPacket().Set(netPeer.SessionToken);
        try
        {
            var sessionToken = httpNetPeer.SessionToken;
            httpNetPeer.SetConnectionState(McApiConnectionState.Disconnected);
            httpNetPeer.SetSessionToken("");
            OnPeerDisconnected?.Invoke(httpNetPeer, sessionToken);
            if (force) return;

            lock (_writer)
            {
                _writer.Reset();
                _writer.Put((byte)logoutPacket.PacketType);
                _writer.Put(logoutPacket);
                if (_writer.Length > short.MaxValue)
                    throw new ArgumentOutOfRangeException(nameof(netPeer));

                var encodedPacket = Z85.GetStringWithPadding(_writer.AsReadOnlySpan());
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
            httpNetPeer.SetSessionToken(Guid.NewGuid().ToString());
            httpNetPeer.SetConnectionState(McApiConnectionState.Connected);
            SendPacket(httpNetPeer,
                PacketPool<McApiAcceptResponsePacket>.GetPacket().Set(packet.RequestId, httpNetPeer.SessionToken));
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
        var responsePacket = PacketPool<McApiDenyResponsePacket>.GetPacket().Set(packet.RequestId, reason);
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

                var encodedPacket = Z85.GetStringWithPadding(_writer.AsReadOnlySpan());
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
        if (disposing)
        {
            OnPeerConnected = null;
            OnPeerDisconnected = null;
        }
        base.Dispose(disposing);
    }

    private async Task ListenerLoop(HttpListener listener)
    {
        while (listener.IsListening)
        {
            var context = await listener.GetContextAsync();
            await HandleRequest(context);
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

            var packet = await JsonSerializer.DeserializeAsync<McHttpUpdatePacket>(context.Request.InputStream);
            if (packet == null)
            {
                context.Response.StatusCode = 400;
                context.Response.Close();
                return;
            }

            var netPeer = GetOrCreatePeer(context.Request.RemoteEndPoint);
            ReceivePacketsLogic(netPeer, packet.Packets, token);
            packet.Packets.Clear();
            SendPacketsLogic(netPeer, packet.Packets);

            var responseData = JsonSerializer.Serialize(packet);
            var buffer = Encoding.UTF8.GetBytes(responseData);
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

    private HttpMcApiNetPeer GetOrCreatePeer(IPEndPoint endPoint)
    {
        return _mcApiPeers.GetOrAdd(endPoint, _ =>
        {
            var httpNetPeer = new HttpMcApiNetPeer(endPoint);
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
                packets.Add(packet.Data);
            }
            catch
            {
                //Do Nothing
            }
        }
    }

    private void UpdatePeer(IPEndPoint endPoint, HttpMcApiNetPeer httpNetPeer)
    {
        lock (_reader)
        {
            while (httpNetPeer.IncomingQueue.TryDequeue(out var packet))
                try
                {
                    _reader.Clear();
                    _reader.SetSource(Z85.GetBytesWithPadding(packet.Data));
                    ProcessPacket(_reader, mcApiPacket =>
                    {
                        if (Config.DisabledPacketTypes.Contains(mcApiPacket.PacketType)) return;
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
        _mcApiPeers.TryRemove(endPoint, out _);
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