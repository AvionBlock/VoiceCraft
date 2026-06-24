using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using LiteNetLib.Utils;
using VoiceCraft.Core.JsonConverters;
using VoiceCraft.Core.World;
using VoiceCraft.Network.NetPeers;
using VoiceCraft.Network.Packets.McApiPackets.Request;
using VoiceCraft.Network.Packets.McApiPackets.Response;
using VoiceCraft.Network.Systems;

namespace VoiceCraft.Network.Servers;

public class TcpMcApiServer(VoiceCraftWorld world, AudioEffectSystem audioEffectSystem)
    : McApiServer(world, audioEffectSystem)
{
    private const int FrameHeaderSize = 12;
    private const int MaxFramePayloadLength = 1024 * 1024;
    private const int FrameMagic = 0x4D435450;
    private const ushort FrameVersion = 1;
    private const ushort RequestKind = 1;
    private const ushort ResponseKind = 2;

    private McTcpConfig _config = new();
    private volatile ImmutableList<McApiNetPeer> _peersSnapshot = ImmutableList<McApiNetPeer>.Empty;
    private readonly Dictionary<TcpClient, TcpMcApiNetPeer> _mcApiPeers = new();
    private readonly NetDataReader _reader = new();
    private readonly NetDataWriter _writer = new();
    private readonly Lock _lock = new();
    private TcpListener? _listener;
    private CancellationTokenSource? _listenerCancellationTokenSource;

    private readonly record struct RentedFramePayload(byte[] Buffer, int Length);

    public McTcpConfig Config
    {
        get => _config;
        set
        {
            if (_listener != null)
                throw new InvalidOperationException();
            _config = value;
        }
    }

    public override string LoginToken => _config.LoginToken;
    public override uint MaxClients => _config.MaxClients;
    public override int ConnectedPeers => Peers.Count(x => x.ConnectionState == McApiConnectionState.Connected);
    public override ImmutableList<McApiNetPeer> Peers => _peersSnapshot;

    public override event Action<McApiNetPeer, string>? OnPeerConnected;
    public override event Action<McApiNetPeer, string>? OnPeerDisconnected;

    public override void Start()
    {
        lock (_lock)
        {
            Stop();
            var ipAddress = ResolveBindAddress(_config.Hostname);
            _listenerCancellationTokenSource = new CancellationTokenSource();
            _listener = new TcpListener(ipAddress, _config.Port);
            _listener.Server.NoDelay = true;
            _listener.Start((int)_config.MaxClients);
        }

        _ = ListenerLoopAsync(_listener, _listenerCancellationTokenSource.Token);
    }

    public override void Update()
    {
        //Cache Snapshot
        var snapshot = _peersSnapshot;
        if (_listener == null) return;
        foreach (var peer in snapshot.Cast<TcpMcApiNetPeer>()) UpdatePeer(peer);
    }

    public override void Stop()
    {
        lock (_lock)
        {
            var snapshot = _peersSnapshot;
            _listenerCancellationTokenSource?.Cancel();
            _listenerCancellationTokenSource?.Dispose();
            _listenerCancellationTokenSource = null;

            if (_listener == null)
            {
                ClearTcpPeers();
                return;
            }

            try
            {
                _listener.Stop();
                _listener.Dispose();
            }
            catch
            {
                // Do Nothing
            }

            ClearTcpPeers();
            foreach (var peer in snapshot)
                try
                {
                    Disconnect(peer, true);
                }
                catch
                {
                    //Do Nothing
                }

            _listener = null;
        }
    }

    public override void SendPacket<T>(McApiNetPeer netPeer, T packet)
    {
        if (_listener == null ||
            netPeer.Server != this ||
            netPeer.ConnectionState == McApiConnectionState.Disconnected ||
            Config.DisabledPacketTypes.Contains(packet.PacketType)) return;
        lock (_writer)
        {
            _writer.Reset();
            _writer.Put((byte)packet.PacketType);
            _writer.Put(packet);
            if (_writer.Length > short.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(packet));

            if (netPeer is not TcpMcApiNetPeer tcpNetPeer)
                return;

            tcpNetPeer.OutgoingQueue.Enqueue(new McApiNetPeer.QueuedPacket(_writer.CopyData(), string.Empty));
        }
    }

    public override void Broadcast<T>(T packet, params McApiNetPeer?[] excludes)
    {
        if (_listener == null || Config.DisabledPacketTypes.Contains(packet.PacketType)) return;
        var snapshot = _peersSnapshot;
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

        foreach (var netPeer in snapshot.Where(netPeer =>
                     netPeer.ConnectionState == McApiConnectionState.Connected && !excludes.Contains(netPeer)))
        {
            netPeer.OutgoingQueue.Enqueue(new McApiNetPeer.QueuedPacket(data, string.Empty));
        }
    }

    public override void Disconnect(McApiNetPeer netPeer, bool force = false)
    {
        if (netPeer.Server != this || netPeer is not TcpMcApiNetPeer tcpNetPeer) return; //Not our client.
        if (netPeer.ConnectionState is McApiConnectionState.Disconnected or McApiConnectionState.Disconnecting)
        {
            //Already disconnected or disconnecting, we can just force closure of the client.
            //The original disconnection call or thread will raise the event.
            if (!force) return;
            TryRemoveTcpPeer(tcpNetPeer.Client, out _);
            CloseClient(tcpNetPeer.Client);
            tcpNetPeer.CancelPendingResponse(); //Cancel any responses, We are force closing.
            return;
        }

        var wasConnected = tcpNetPeer.ConnectionState == McApiConnectionState.Connected;
        tcpNetPeer.ConnectionState = McApiConnectionState.Disconnecting;
        var sessionToken = tcpNetPeer.SessionToken;
        var logoutPacket = PacketPool<McApiLogoutRequestPacket>.GetPacket(() => new McApiLogoutRequestPacket());
        try
        {
            if (force)
            {
                TryRemoveTcpPeer(tcpNetPeer.Client, out _);
                CloseClient(tcpNetPeer.Client);
                tcpNetPeer.CancelPendingResponse(); //Cancel any responses, We are force closing.
                return;
            }

            logoutPacket.Set(netPeer.SessionToken);
            SendPacket(netPeer, logoutPacket);
            // We don't close the client forcefully,
            // We keep the connection open in case of a re-connect without doing the TCP handshake.
            // The TCP connection will close when it fully times out.
        }
        finally
        {
            logoutPacket.Return();
            tcpNetPeer.SetSessionToken(string.Empty);
            tcpNetPeer.ConnectionState = McApiConnectionState.Disconnected;
            if (wasConnected)
                OnPeerDisconnected?.Invoke(tcpNetPeer, sessionToken);
        }
    }

    protected override void AcceptRequest(McApiLoginRequestPacket packet, McApiNetPeer netPeer)
    {
        if (netPeer is not TcpMcApiNetPeer tcpNetPeer) return;
        var acceptPacket = PacketPool<McApiAcceptResponsePacket>.GetPacket(() => new McApiAcceptResponsePacket());
        try
        {
            if (tcpNetPeer.ConnectionState != McApiConnectionState.Connected)
                tcpNetPeer.SetSessionToken(Guid.NewGuid().ToString());

            acceptPacket.Set(packet.RequestId, netPeer.SessionToken);
            SendPacket(tcpNetPeer, acceptPacket);

            tcpNetPeer.ConnectionState = McApiConnectionState.Connected;
            OnPeerConnected?.Invoke(tcpNetPeer, tcpNetPeer.SessionToken);
        }
        catch
        {
            RejectRequest(packet, "McApi.DisconnectReason.Error", tcpNetPeer);
        }
        finally
        {
            acceptPacket.Return();
        }
    }

    protected override void RejectRequest(McApiLoginRequestPacket packet, string reason, McApiNetPeer netPeer)
    {
        if (netPeer is not TcpMcApiNetPeer tcpNetPeer) return;
        var denyPacket = PacketPool<McApiDenyResponsePacket>.GetPacket(() => new McApiDenyResponsePacket());
        try
        {
            denyPacket.Set(packet.RequestId, reason);
            SendPacket(tcpNetPeer, denyPacket);
        }
        finally
        {
            denyPacket.Return();
            tcpNetPeer.SetSessionToken(string.Empty);
            tcpNetPeer.ConnectionState = McApiConnectionState.Disconnected;
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

    private async Task ListenerLoopAsync(TcpListener listener, CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var client = await listener.AcceptTcpClientAsync(cancellationToken);
                OnClientConnected(client);
                _ = HandleClientAsync(client, cancellationToken);
            }
        }
        catch
        {
            //Do Nothing
        }
    }

    private async Task HandleClientAsync(TcpClient client, CancellationToken cancellationToken)
    {
        var headerBuffer = ArrayPool<byte>.Shared.Rent(FrameHeaderSize);
        try
        {
            await using var stream = client.GetStream();
            while (!cancellationToken.IsCancellationRequested && client.Connected)
            {
                var framePayload = await ReadFrameAsync(stream, headerBuffer, cancellationToken);
                if (framePayload == null)
                    break;

                var payload = framePayload.Value;
                try
                {
                    if (!TryGetTcpPeer(client, out var peer))
                        break;

                    if (!TryReadPayload(payload.Buffer.AsSpan(0, payload.Length), out var token, out var packets))
                        break;

                    var responseTask = peer.CreatePendingResponseTask();
                    ReceivePacketsLogic(peer, packets, token);
                    var responsePackets = await responseTask.WaitAsync(cancellationToken);
                    await WriteFrameAsync(stream, string.Empty, responsePackets, cancellationToken);
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(payload.Buffer);
                }
            }
        }
        catch
        {
            //Do Nothing
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(headerBuffer);
            OnClientDisconnected(client);
        }
    }

    private static void ReceivePacketsLogic(TcpMcApiNetPeer tcpNetPeer, IReadOnlyList<byte[]> packets, string token)
    {
        foreach (var data in packets)
        {
            if (data.Length == 0) continue;
            try
            {
                tcpNetPeer.IncomingQueue.Enqueue(new McApiNetPeer.QueuedPacket(data, token));
            }
            catch
            {
                // Do Nothing
            }
        }
    }

    private static void SendPacketsLogic(TcpMcApiNetPeer netPeer)
    {
        if (!netPeer.HasPendingResponse()) return;

        var packets = new List<byte[]>();
        while (netPeer.OutgoingQueue.TryDequeue(out var packet))
        {
            try
            {
                packets.Add(packet.Data);
            }
            catch
            {
                // Do Nothing
            }
        }

        netPeer.CompletePendingResponse(packets);
    }

    private void UpdatePeer(TcpMcApiNetPeer tcpNetPeer)
    {
        ProcessPackets(tcpNetPeer);
        SendPacketsLogic(tcpNetPeer);
        if (DateTime.UtcNow - tcpNetPeer.LastUpdate < TimeSpan.FromMilliseconds(Config.MaxTimeoutMs)) return;
        //Double the amount of time. We remove the peer.
        if (DateTime.UtcNow - tcpNetPeer.LastUpdate < TimeSpan.FromMilliseconds(Config.MaxTimeoutMs * 2)) return;
        Disconnect(tcpNetPeer, true);
    }

    private void ProcessPackets(TcpMcApiNetPeer tcpNetPeer)
    {
        lock (_reader)
        {
            while (tcpNetPeer.IncomingQueue.TryDequeue(out var packet))
                try
                {
                    var packetToken = packet.Token;
                    _reader.Clear();
                    _reader.SetSource(packet.Data);
                    ProcessPacket(_reader, mcApiPacket =>
                    {
                        tcpNetPeer.LastUpdate = DateTime.UtcNow;
                        if (!AuthorizePacket(mcApiPacket, tcpNetPeer, packetToken) ||
                            Config.DisabledPacketTypes.Contains(mcApiPacket.PacketType)) return;
                        ExecutePacket(mcApiPacket, tcpNetPeer);
                    });
                }
                catch
                {
                    // Do Nothing
                }
        }
    }

    private void OnClientConnected(TcpClient client)
    {
        var count = _peersSnapshot.Count;
        if (count >= Config.MaxClients)
        {
            CloseClient(client); //Full
            return;
        }

        client.NoDelay = true;
        var netPeer = new TcpMcApiNetPeer(this, client);
        if (!TryAddTcpPeer(client, netPeer))
            CloseClient(client); //Error
    }

    private void OnClientDisconnected(TcpClient client)
    {
        if (!TryRemoveTcpPeer(client, out var mcApiPeer))
        {
            CloseClient(client);
            return;
        }

        var wasConnected = mcApiPeer.ConnectionState != McApiConnectionState.Disconnected;
        var sessionToken = mcApiPeer.SessionToken;
        mcApiPeer.SetSessionToken(string.Empty);
        mcApiPeer.ConnectionState = McApiConnectionState.Disconnected;
        mcApiPeer.CancelPendingResponse();
        if (wasConnected)
            OnPeerDisconnected?.Invoke(mcApiPeer, sessionToken);

        CloseClient(client);
    }

    private bool TryAddTcpPeer(TcpClient client, TcpMcApiNetPeer peer)
    {
        lock (_lock)
        {
            if (!_mcApiPeers.TryAdd(client, peer)) return false;
            _peersSnapshot = [.._mcApiPeers.Values];
            return true;
        }
    }

    private bool TryRemoveTcpPeer(TcpClient client, [NotNullWhen(true)] out TcpMcApiNetPeer? peer)
    {
        lock (_lock)
        {
            if (!_mcApiPeers.Remove(client, out peer)) return false;
            _peersSnapshot = [.._mcApiPeers.Values];
            return true;
        }
    }

    private bool TryGetTcpPeer(TcpClient client, [NotNullWhen(true)] out TcpMcApiNetPeer? peer)
    {
        lock (_lock)
        {
            return _mcApiPeers.TryGetValue(client, out peer);
        }
    }

    private void ClearTcpPeers()
    {
        lock (_lock)
        {
            _mcApiPeers.Clear();
            _peersSnapshot = ImmutableList<McApiNetPeer>.Empty;
        }
    }

    private static async Task<RentedFramePayload?> ReadFrameAsync(
        NetworkStream stream,
        byte[] headerBuffer,
        CancellationToken cancellationToken)
    {
        if (!await ReadExactAsync(stream, headerBuffer.AsMemory(0, FrameHeaderSize), cancellationToken))
            return null;

        var magic = BinaryPrimitives.ReadInt32BigEndian(headerBuffer.AsSpan(0, 4));
        var version = BinaryPrimitives.ReadUInt16BigEndian(headerBuffer.AsSpan(4, 2));
        var kind = BinaryPrimitives.ReadUInt16BigEndian(headerBuffer.AsSpan(6, 2));
        var payloadLength = BinaryPrimitives.ReadInt32BigEndian(headerBuffer.AsSpan(8, 4));

        if (magic != FrameMagic || version != FrameVersion || kind != RequestKind)
            return null;
        if (payloadLength is < 0 or > MaxFramePayloadLength)
            return null;

        var payloadBuffer = ArrayPool<byte>.Shared.Rent(payloadLength);
        if (await ReadExactAsync(stream, payloadBuffer.AsMemory(0, payloadLength), cancellationToken))
            return new RentedFramePayload(payloadBuffer, payloadLength);
        ArrayPool<byte>.Shared.Return(payloadBuffer);
        return null;
    }

    private static async Task WriteFrameAsync(
        NetworkStream stream,
        string token,
        IReadOnlyList<byte[]> packets,
        CancellationToken cancellationToken)
    {
        var tokenLength = string.IsNullOrEmpty(token) ? 0 : Encoding.UTF8.GetByteCount(token);
        var payloadLength = 4 + tokenLength + 4;
        foreach (var packet in packets)
            payloadLength += 4 + packet.Length;

        if (payloadLength > MaxFramePayloadLength)
            throw new InvalidOperationException("McTcp response payload exceeds the maximum frame size.");

        var frameLength = FrameHeaderSize + payloadLength;
        var frameBuffer = ArrayPool<byte>.Shared.Rent(frameLength);
        try
        {
            BinaryPrimitives.WriteInt32BigEndian(frameBuffer.AsSpan(0, 4), FrameMagic);
            BinaryPrimitives.WriteUInt16BigEndian(frameBuffer.AsSpan(4, 2), FrameVersion);
            BinaryPrimitives.WriteUInt16BigEndian(frameBuffer.AsSpan(6, 2), ResponseKind);
            BinaryPrimitives.WriteInt32BigEndian(frameBuffer.AsSpan(8, 4), payloadLength);

            var offset = FrameHeaderSize;
            WriteInt32(frameBuffer, ref offset, tokenLength);
            if (tokenLength > 0)
                offset += Encoding.UTF8.GetBytes(token, frameBuffer.AsSpan(offset, tokenLength));

            WriteInt32(frameBuffer, ref offset, packets.Count);
            foreach (var packet in packets)
            {
                WriteInt32(frameBuffer, ref offset, packet.Length);
                packet.CopyTo(frameBuffer.AsSpan(offset));
                offset += packet.Length;
            }

            await stream.WriteAsync(frameBuffer.AsMemory(0, frameLength), cancellationToken);
            await stream.FlushAsync(cancellationToken);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(frameBuffer);
        }
    }

    private static bool TryReadPayload(ReadOnlySpan<byte> payload, out string token, out List<byte[]> packets)
    {
        token = string.Empty;
        packets = [];
        if (payload.Length < 8)
            return false;

        var offset = 0;
        if (!TryReadInt32(payload, ref offset, out var tokenLength) || tokenLength < 0 ||
            payload.Length - offset < tokenLength)
            return false;

        token = tokenLength == 0 ? string.Empty : Encoding.UTF8.GetString(payload.Slice(offset, tokenLength));
        offset += tokenLength;

        if (!TryReadInt32(payload, ref offset, out var packetCount) || packetCount < 0)
            return false;
        if (packetCount > (payload.Length - offset) / 5)
            return false;

        packets = new List<byte[]>(packetCount);
        for (var i = 0; i < packetCount; i++)
        {
            if (!TryReadInt32(payload, ref offset, out var packetLength) || packetLength <= 0 ||
                payload.Length - offset < packetLength)
                return false;

            var packet = new byte[packetLength];
            payload.Slice(offset, packetLength).CopyTo(packet);
            packets.Add(packet);
            offset += packetLength;
        }

        return offset == payload.Length;
    }

    private static bool TryReadInt32(ReadOnlySpan<byte> payload, ref int offset, out int value)
    {
        value = 0;
        if (payload.Length - offset < 4)
            return false;

        value = BinaryPrimitives.ReadInt32BigEndian(payload.Slice(offset, 4));
        offset += 4;
        return true;
    }

    private static void WriteInt32(byte[] payload, ref int offset, int value)
    {
        BinaryPrimitives.WriteInt32BigEndian(payload.AsSpan(offset, 4), value);
        offset += 4;
    }

    private static async ValueTask<bool> ReadExactAsync(NetworkStream stream, Memory<byte> buffer,
        CancellationToken cancellationToken)
    {
        var offset = 0;
        while (offset < buffer.Length)
        {
            var read = await stream.ReadAsync(buffer[offset..], cancellationToken);
            if (read == 0)
                return false;
            offset += read;
        }

        return true;
    }

    private static IPAddress ResolveBindAddress(string configuredHostname)
    {
        if (IPAddress.TryParse(configuredHostname, out var ipAddress))
            return ipAddress;

        var addresses = Dns.GetHostAddresses(configuredHostname);
        var ipv4Address = addresses.FirstOrDefault(x => x.AddressFamily == AddressFamily.InterNetwork);
        if (ipv4Address != null)
            return ipv4Address;

        throw new InvalidOperationException(
            $"McTcp cannot resolve bind hostname '{configuredHostname}' to an IPv4 address.");
    }

    private static void CloseClient(TcpClient client)
    {
        try
        {
            client.Close();
        }
        catch
        {
            //Do Nothing
        }
    }

    public class McTcpConfig
    {
        [JsonConverter(typeof(JsonBooleanConverter))]
        public bool Enabled { get; set; }

        public string LoginToken { get; set; } = Guid.NewGuid().ToString();
        public string Hostname { get; set; } = "127.0.0.1";
        public int Port { get; set; } = 9050;
        public uint ExternalPort { get; set; }
        public uint PortMappingLifetimeMinutes { get; set; } = 60;
        public uint PortMappingTimeoutSeconds { get; set; } = 5;
        public uint MaxClients { get; set; } = 1;
        public uint MaxTimeoutMs { get; set; } = 10000;

        [JsonConverter(typeof(JsonBooleanConverter))]
        public bool AutoOpenPort { get; set; }

        public HashSet<McApiPacketType> DisabledPacketTypes { get; set; } = [];
    }
}