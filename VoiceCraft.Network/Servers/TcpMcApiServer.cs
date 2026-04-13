using System;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
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
    private readonly ConcurrentDictionary<TcpClient, TcpMcApiNetPeer> _mcApiPeers = new();
    private readonly NetDataReader _reader = new();
    private readonly NetDataWriter _writer = new();
    private TcpListener? _listener;
    private CancellationTokenSource? _listenerCancellationTokenSource;

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

    public override int ConnectedPeers =>
        _mcApiPeers.Count(x => x.Value.ConnectionState == McApiConnectionState.Connected);

    public override event Action<McApiNetPeer, string>? OnPeerConnected;
    public override event Action<McApiNetPeer, string>? OnPeerDisconnected;

    public override void Start()
    {
        Stop();
        var ipAddress = ResolveBindAddress(_config.Hostname);
        _listenerCancellationTokenSource = new CancellationTokenSource();
        _listener = new TcpListener(ipAddress, _config.Port);
        _listener.Server.NoDelay = true;
        _listener.Start((int)_config.MaxClients);
        _ = ListenerLoopAsync(_listener, _listenerCancellationTokenSource.Token);
    }

    public override void Update()
    {
        if (_listener == null) return;
        foreach (var peer in _mcApiPeers)
            UpdatePeer(peer.Key, peer.Value);
    }

    public override void Stop()
    {
        _listenerCancellationTokenSource?.Cancel();
        _listenerCancellationTokenSource?.Dispose();
        _listenerCancellationTokenSource = null;

        if (_listener != null)
        {
            try
            {
                _listener.Stop();
            }
            catch (SocketException)
            {
                // Do Nothing
            }

            _listener = null;
        }

        foreach (var peer in _mcApiPeers.Keys)
            CloseClient(peer);

        _mcApiPeers.Clear();
    }

    public override void SendPacket<T>(McApiNetPeer netPeer, T packet)
    {
        if (_listener == null || netPeer.ConnectionState != McApiConnectionState.Connected ||
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

                if (netPeer is not TcpMcApiNetPeer tcpNetPeer)
                    return;

                tcpNetPeer.OutgoingRawQueue.Enqueue(_writer.CopyData());
            }
        }
        finally
        {
            PacketPool<T>.Return(packet);
        }
    }

    public override void Broadcast<T>(T packet, params McApiNetPeer?[] excludes)
    {
        if (_listener == null || Config.DisabledPacketTypes.Contains(packet.PacketType)) return;
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

                var encodedPacket = _writer.CopyData();
                foreach (var netPeer in netPeers)
                {
                    if (excludes.Contains(netPeer.Value)) continue;
                    netPeer.Value.OutgoingRawQueue.Enqueue(encodedPacket);
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
        if (netPeer is not TcpMcApiNetPeer tcpNetPeer) return;
        var sessionToken = tcpNetPeer.SessionToken;
        var wasConnected = tcpNetPeer.ConnectionState == McApiConnectionState.Connected;
        tcpNetPeer.SetConnectionState(McApiConnectionState.Disconnected);
        tcpNetPeer.SetSessionToken(string.Empty);
        tcpNetPeer.CancelPendingResponse();
        if (wasConnected)
            OnPeerDisconnected?.Invoke(tcpNetPeer, sessionToken);

        _mcApiPeers.TryRemove(tcpNetPeer.Client, out _);
        CloseClient(tcpNetPeer.Client);
    }

    protected override void AcceptRequest(McApiLoginRequestPacket packet, object? data)
    {
        if (data is not TcpMcApiNetPeer tcpNetPeer) return;
        try
        {
            if (tcpNetPeer.ConnectionState != McApiConnectionState.Connected)
            {
                tcpNetPeer.SetSessionToken(Guid.NewGuid().ToString());
                tcpNetPeer.SetConnectionState(McApiConnectionState.Connected);
            }

            SendPacket(tcpNetPeer,
                PacketPool<McApiAcceptResponsePacket>.GetPacket(() => new McApiAcceptResponsePacket())
                    .Set(packet.RequestId, tcpNetPeer.SessionToken));
            OnPeerConnected?.Invoke(tcpNetPeer, tcpNetPeer.SessionToken);
        }
        catch
        {
            RejectRequest(packet, "McApi.DisconnectReason.Error", tcpNetPeer);
        }
    }

    protected override void RejectRequest(McApiLoginRequestPacket packet, string reason, object? data)
    {
        if (data is not TcpMcApiNetPeer tcpNetPeer) return;
        var responsePacket = PacketPool<McApiDenyResponsePacket>.GetPacket(() => new McApiDenyResponsePacket())
            .Set(packet.RequestId, reason);
        try
        {
            tcpNetPeer.SetSessionToken(string.Empty);
            tcpNetPeer.SetConnectionState(McApiConnectionState.Disconnected);
            lock (_writer)
            {
                _writer.Reset();
                _writer.Put((byte)responsePacket.PacketType);
                _writer.Put(responsePacket);
                if (_writer.Length > short.MaxValue)
                    throw new ArgumentOutOfRangeException(nameof(packet));

                tcpNetPeer.OutgoingRawQueue.Enqueue(_writer.CopyData());
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
        catch (OperationCanceledException)
        {
            // Do Nothing
        }
        catch (ObjectDisposedException)
        {
            // Do Nothing
        }
        catch (SocketException)
        {
            // Do Nothing
        }
    }

    private async Task HandleClientAsync(TcpClient client, CancellationToken cancellationToken)
    {
        try
        {
            using var stream = client.GetStream();
            while (!cancellationToken.IsCancellationRequested && client.Connected)
            {
                var payload = await ReadFrameAsync(stream, cancellationToken);
                if (payload == null)
                    break;

                if (!_mcApiPeers.TryGetValue(client, out var peer))
                    break;

                if (!TryReadPayload(payload, out var token, out var packets))
                    break;

                var responseTask = peer.CreatePendingResponseTask();
                ReceivePacketsLogic(peer, packets, token);
                var responsePackets = await responseTask.WaitAsync(cancellationToken);
                var response = WritePayload(string.Empty, responsePackets);
                await WriteFrameAsync(stream, response, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Do Nothing
        }
        catch (IOException)
        {
            // Do Nothing
        }
        catch (ObjectDisposedException)
        {
            // Do Nothing
        }
        finally
        {
            OnClientDisconnected(client);
        }
    }

    private void OnClientConnected(TcpClient client)
    {
        if (_mcApiPeers.Count >= Config.MaxClients)
        {
            CloseClient(client);
            return;
        }

        client.NoDelay = true;
        var netPeer = new TcpMcApiNetPeer(client)
        {
            Tag = this
        };
        _mcApiPeers.TryAdd(client, netPeer);
    }

    private void OnClientDisconnected(TcpClient client)
    {
        if (!_mcApiPeers.TryRemove(client, out var mcApiPeer))
        {
            CloseClient(client);
            return;
        }

        if (mcApiPeer.ConnectionState == McApiConnectionState.Connected)
        {
            var sessionToken = mcApiPeer.SessionToken;
            mcApiPeer.SetConnectionState(McApiConnectionState.Disconnected);
            mcApiPeer.SetSessionToken(string.Empty);
            mcApiPeer.CancelPendingResponse();
            OnPeerDisconnected?.Invoke(mcApiPeer, sessionToken);
        }

        CloseClient(client);
    }

    private void UpdatePeer(TcpClient client, TcpMcApiNetPeer tcpNetPeer)
    {
        ProcessIncomingPackets(tcpNetPeer);
        CompletePendingResponse(tcpNetPeer);

        if (DateTime.UtcNow - tcpNetPeer.LastUpdate < TimeSpan.FromMilliseconds(Config.MaxTimeoutMs)) return;
        if (_mcApiPeers.TryRemove(client, out _))
            Disconnect(tcpNetPeer, true);
    }

    private void ProcessIncomingPackets(TcpMcApiNetPeer tcpNetPeer)
    {
        lock (_reader)
        {
            while (tcpNetPeer.IncomingRawQueue.TryDequeue(out var packet))
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

    private static void CompletePendingResponse(TcpMcApiNetPeer netPeer)
    {
        if (!netPeer.HasPendingResponse()) return;

        var packets = new List<byte[]>();
        while (netPeer.OutgoingRawQueue.TryDequeue(out var packet))
        {
            try
            {
                packets.Add(packet);
            }
            catch
            {
                // Do Nothing
            }
        }

        netPeer.CompletePendingResponse(packets);
    }

    private static void ReceivePacketsLogic(TcpMcApiNetPeer tcpNetPeer, IReadOnlyList<byte[]> packets, string token)
    {
        foreach (var data in packets.Where(data => data.Length > 0))
        {
            try
            {
                tcpNetPeer.IncomingRawQueue.Enqueue(new TcpMcApiNetPeer.QueuedRawPacket(data, token));
            }
            catch
            {
                // Do Nothing
            }
        }
    }

    private static async Task<byte[]?> ReadFrameAsync(NetworkStream stream, CancellationToken cancellationToken)
    {
        var headerBuffer = new byte[FrameHeaderSize];
        if (!await ReadExactAsync(stream, headerBuffer, cancellationToken))
            return null;

        var magic = BinaryPrimitives.ReadInt32BigEndian(headerBuffer.AsSpan(0, 4));
        var version = BinaryPrimitives.ReadUInt16BigEndian(headerBuffer.AsSpan(4, 2));
        var kind = BinaryPrimitives.ReadUInt16BigEndian(headerBuffer.AsSpan(6, 2));
        var payloadLength = BinaryPrimitives.ReadInt32BigEndian(headerBuffer.AsSpan(8, 4));

        if (magic != FrameMagic || version != FrameVersion || kind != RequestKind)
            return null;
        if (payloadLength is < 0 or > MaxFramePayloadLength)
            return null;

        var payloadBuffer = new byte[payloadLength];
        if (!await ReadExactAsync(stream, payloadBuffer, cancellationToken))
            return null;

        return payloadBuffer;
    }

    private static async Task WriteFrameAsync(NetworkStream stream, byte[] payloadBuffer, CancellationToken cancellationToken)
    {
        if (payloadBuffer.Length > MaxFramePayloadLength)
            throw new InvalidOperationException("McTcp response payload exceeds the maximum frame size.");

        var frameBuffer = new byte[FrameHeaderSize + payloadBuffer.Length];
        BinaryPrimitives.WriteInt32BigEndian(frameBuffer.AsSpan(0, 4), FrameMagic);
        BinaryPrimitives.WriteUInt16BigEndian(frameBuffer.AsSpan(4, 2), FrameVersion);
        BinaryPrimitives.WriteUInt16BigEndian(frameBuffer.AsSpan(6, 2), ResponseKind);
        BinaryPrimitives.WriteInt32BigEndian(frameBuffer.AsSpan(8, 4), payloadBuffer.Length);
        payloadBuffer.CopyTo(frameBuffer.AsSpan(FrameHeaderSize));

        await stream.WriteAsync(frameBuffer, cancellationToken);
        await stream.FlushAsync(cancellationToken);
    }

    private static bool TryReadPayload(byte[] payload, out string token, out List<byte[]> packets)
    {
        token = string.Empty;
        packets = [];
        if (payload.Length < 8)
            return false;

        var offset = 0;
        if (!TryReadInt32(payload, ref offset, out var tokenLength) || tokenLength < 0 || payload.Length - offset < tokenLength)
            return false;

        token = tokenLength == 0 ? string.Empty : Encoding.UTF8.GetString(payload, offset, tokenLength);
        offset += tokenLength;

        if (!TryReadInt32(payload, ref offset, out var packetCount) || packetCount < 0)
            return false;

        packets = new List<byte[]>(packetCount);
        for (var i = 0; i < packetCount; i++)
        {
            if (!TryReadInt32(payload, ref offset, out var packetLength) || packetLength <= 0 ||
                payload.Length - offset < packetLength)
                return false;

            var packet = new byte[packetLength];
            Buffer.BlockCopy(payload, offset, packet, 0, packetLength);
            packets.Add(packet);
            offset += packetLength;
        }

        return offset == payload.Length;
    }

    private static byte[] WritePayload(string token, IReadOnlyList<byte[]> packets)
    {
        var tokenBytes = string.IsNullOrEmpty(token) ? [] : Encoding.UTF8.GetBytes(token);
        var payloadLength = 4 + tokenBytes.Length + 4;
        foreach (var packet in packets)
            payloadLength += 4 + packet.Length;

        var payload = new byte[payloadLength];
        var offset = 0;
        WriteInt32(payload, ref offset, tokenBytes.Length);
        if (tokenBytes.Length > 0)
        {
            tokenBytes.CopyTo(payload, offset);
            offset += tokenBytes.Length;
        }

        WriteInt32(payload, ref offset, packets.Count);
        foreach (var packet in packets)
        {
            WriteInt32(payload, ref offset, packet.Length);
            packet.CopyTo(payload, offset);
            offset += packet.Length;
        }

        return payload;
    }

    private static bool TryReadInt32(byte[] payload, ref int offset, out int value)
    {
        value = 0;
        if (payload.Length - offset < 4)
            return false;

        value = BinaryPrimitives.ReadInt32BigEndian(payload.AsSpan(offset, 4));
        offset += 4;
        return true;
    }

    private static void WriteInt32(byte[] payload, ref int offset, int value)
    {
        BinaryPrimitives.WriteInt32BigEndian(payload.AsSpan(offset, 4), value);
        offset += 4;
    }

    private static async ValueTask<bool> ReadExactAsync(NetworkStream stream, byte[] buffer,
        CancellationToken cancellationToken)
    {
        var offset = 0;
        while (offset < buffer.Length)
        {
            var read = await stream.ReadAsync(buffer.AsMemory(offset, buffer.Length - offset), cancellationToken);
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
        catch (SocketException)
        {
            // Do Nothing
        }
        catch (ObjectDisposedException)
        {
            // Do Nothing
        }
    }

    public class McTcpConfig
    {
        [JsonConverter(typeof(JsonBooleanConverter))]
        public bool Enabled { get; set; }

        public string LoginToken { get; set; } = Guid.NewGuid().ToString();
        public string Hostname { get; set; } = "127.0.0.1";
        public int Port { get; set; } = 9050;
        public uint MaxClients { get; set; } = 1;
        public uint MaxTimeoutMs { get; set; } = 10000;
        public HashSet<McApiPacketType> DisabledPacketTypes { get; set; } = [];
    }
}
