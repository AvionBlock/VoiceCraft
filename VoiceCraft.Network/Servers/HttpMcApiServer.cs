using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
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
    private const int MaxRequestLength = 1_000_000;

    private HttpMcApiConfig _config = new();
    private readonly ConcurrentDictionary<string, HttpMcApiNetPeer> _mcApiPeers = new();
    private readonly ConcurrentQueue<PendingHttpRequest> _pendingRequests = new();
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
                $"McHttp cannot bind to '{_config.Hostname}' due to insufficient permissions for this address/port.", ex);
        }

        _ = ListenerLoop(_httpServer);
    }

    public override void Update()
    {
        if (_httpServer == null) return;
        ProcessPendingRequests();
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
            lock (_writer)
            {
                _writer.Reset();
                _writer.Put((byte)packet.PacketType);
                _writer.Put(packet);
                if (_writer.Length > short.MaxValue)
                    throw new ArgumentOutOfRangeException(nameof(packet));

                var data = _writer.CopyData();
                foreach (var netPeer in _mcApiPeers.Values)
                {
                    if (netPeer.ConnectionState != McApiConnectionState.Connected || excludes.Contains(netPeer))
                        continue;
                    netPeer.OutgoingQueue.Enqueue(new McApiNetPeer.QueuedPacket(data, string.Empty));
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
                if (!string.IsNullOrEmpty(httpNetPeer.LookupToken))
                    _mcApiPeers.TryRemove(httpNetPeer.LookupToken, out _); //Remove Immediately.
                return;
            }

            lock (_writer)
            {
                _writer.Reset();
                _writer.Put((byte)logoutPacket.PacketType);
                _writer.Put(logoutPacket);
                if (_writer.Length > short.MaxValue)
                    throw new ArgumentOutOfRangeException(nameof(netPeer));

                netPeer.OutgoingQueue.Enqueue(new McApiNetPeer.QueuedPacket(_writer.CopyData(), string.Empty));
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
            var previousLookupToken = httpNetPeer.LookupToken;
            if (httpNetPeer.ConnectionState != McApiConnectionState.Connected)
            {
                httpNetPeer.SetSessionToken(Guid.NewGuid().ToString());
                httpNetPeer.SetConnectionState(McApiConnectionState.Connected);
            }

            httpNetPeer.SetLookupToken(httpNetPeer.SessionToken);
            TrackPeer(httpNetPeer, previousLookupToken);

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

                httpNetPeer.OutgoingQueue.Enqueue(new McApiNetPeer.QueuedPacket(_writer.CopyData(), string.Empty));
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

            if (context.Request.ContentLength64 < 0)
            {
                context.Response.StatusCode = 411;
                context.Response.Close();
                return;
            }

            if (context.Request.ContentLength64 > MaxRequestLength) //Do not accept anything higher than a mb.
            {
                context.Response.StatusCode = 413;
                context.Response.Close();
                return;
            }

            if (!TryGetBearerToken(context.Request.Headers.Get("Authorization"), out var token))
            {
                context.Response.StatusCode = 401;
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

            var netPeer = GetOrCreatePeer(context.Request.RemoteEndPoint.Address, token);
            var pendingRequest = new PendingHttpRequest(netPeer, packets, token);
            _pendingRequests.Enqueue(pendingRequest);
            packets = await pendingRequest.CompletionSource.Task;

            byte[] buffer;
            lock (_httpWriter)
            {
                _httpWriter.Reset();
                foreach (var packet in packets)
                {
                    _httpWriter.Put((ushort)packet.Length);
                    _httpWriter.Put(packet);
                }
                var encoded = Z85.GetStringWithPadding(_httpWriter.AsReadOnlySpan());
                buffer = Encoding.UTF8.GetBytes(encoded);
            }

            context.Response.StatusCode = 200;
            context.Response.ContentLength64 = buffer.Length;
            await context.Response.OutputStream.WriteAsync(buffer);
            context.Response.OutputStream.Close();
        }
        catch
        {
            context.Response.StatusCode = 500;
            context.Response.Close();
        }
    }

    private HttpMcApiNetPeer GetOrCreatePeer(IPAddress ipAddress, string token)
    {
        if (_mcApiPeers.TryGetValue(token, out var netPeer))
            return netPeer;

        return new HttpMcApiNetPeer(ipAddress)
        {
            Tag = this
        };
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

    private void ProcessPendingRequests()
    {
        while (_pendingRequests.TryDequeue(out var pendingRequest))
            try
            {
                ProcessPackets(pendingRequest.NetPeer, pendingRequest.Packets, pendingRequest.Token);
                pendingRequest.Packets.Clear();
                SendPacketsLogic(pendingRequest.NetPeer, pendingRequest.Packets);
                pendingRequest.CompletionSource.TrySetResult(pendingRequest.Packets);
            }
            catch (Exception ex)
            {
                pendingRequest.CompletionSource.TrySetException(ex);
            }
    }

    private void ProcessPackets(HttpMcApiNetPeer httpNetPeer, List<byte[]> packets, string token)
    {
        ReceivePacketsLogic(httpNetPeer, packets, token);
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

    private static void SendPacketsLogic(HttpMcApiNetPeer netPeer, List<byte[]> packets)
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
        if (DateTime.UtcNow - httpNetPeer.LastUpdate < TimeSpan.FromMilliseconds(Config.MaxTimeoutMs)) return;
        Disconnect(httpNetPeer);
        //Double the amount of time. We remove the peer.
        if (DateTime.UtcNow - httpNetPeer.LastUpdate < TimeSpan.FromMilliseconds(Config.MaxTimeoutMs * 2)) return;
        _mcApiPeers.TryRemove(lookupToken, out _);
    }

    private void TrackPeer(HttpMcApiNetPeer httpNetPeer, string? previousLookupToken = null)
    {
        if (!string.IsNullOrEmpty(previousLookupToken) && previousLookupToken != httpNetPeer.LookupToken)
            _mcApiPeers.TryRemove(previousLookupToken, out _);

        if (!string.IsNullOrEmpty(httpNetPeer.LookupToken))
            _mcApiPeers[httpNetPeer.LookupToken] = httpNetPeer;
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

    private static bool TryGetBearerToken(string? authorizationHeader, out string token)
    {
        const string bearerPrefix = "Bearer ";

        token = string.Empty;
        if (string.IsNullOrWhiteSpace(authorizationHeader) ||
            !authorizationHeader.StartsWith(bearerPrefix, StringComparison.OrdinalIgnoreCase))
            return false;

        token = authorizationHeader[bearerPrefix.Length..].Trim();
        return !string.IsNullOrWhiteSpace(token);
    }

    private sealed class PendingHttpRequest(HttpMcApiNetPeer netPeer, List<byte[]> packets, string token)
    {
        public HttpMcApiNetPeer NetPeer { get; } = netPeer;
        public List<byte[]> Packets { get; } = packets;
        public string Token { get; } = token;
        public TaskCompletionSource<List<byte[]>> CompletionSource { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
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
