using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using Fleck;
using LiteNetLib.Utils;
using VoiceCraft.Core.JsonConverters;
using VoiceCraft.Core.World;
using VoiceCraft.Network.NetPeers;
using VoiceCraft.Network.Packets.McApiPackets.Request;
using VoiceCraft.Network.Packets.McApiPackets.Response;
using VoiceCraft.Network.Packets.McWssPackets;
using VoiceCraft.Network.Systems;

namespace VoiceCraft.Network.Servers;

public class McWssMcApiServer(VoiceCraftWorld world, AudioEffectSystem audioEffectSystem)
    : McApiServer(world, audioEffectSystem)
{
    private McWssMcApiConfig _config = new();
    private volatile ImmutableList<McApiNetPeer> _peersSnapshot = ImmutableList<McApiNetPeer>.Empty;
    private readonly Dictionary<IWebSocketConnection, McWssMcApiNetPeer> _mcApiPeers = new();
    private readonly NetDataWriter _mcWssWriter = new();
    private readonly NetDataReader _mcWssReader = new();
    private readonly NetDataReader _reader = new();
    private readonly NetDataWriter _writer = new();
    private readonly Lock _lock = new();
    private WebSocketServer? _wsServer;

    public McWssMcApiConfig Config
    {
        get => _config;
        set
        {
            if (_wsServer != null)
                throw new InvalidOperationException();
            _config = value;
        }
    }

    public override uint MaxClients => Config.MaxClients;
    public override string LoginToken => Config.LoginToken;
    public override int ConnectedPeers => Peers.Count(x => x.ConnectionState == McApiConnectionState.Connected);
    public override ImmutableList<McApiNetPeer> Peers => _peersSnapshot;

    public override event Action<McApiNetPeer, string>? OnPeerConnected;
    public override event Action<McApiNetPeer, string>? OnPeerDisconnected;

    public override void Start()
    {
        lock (_lock)
        {
            Stop();
            _wsServer = new WebSocketServer(_config.Hostname);
            _wsServer.Start(socket =>
            {
                socket.OnOpen = () => OnClientConnected(socket);
                socket.OnClose = () => OnClientDisconnected(socket);
                socket.OnMessage = message => OnMessageReceived(socket, message);
            });
        }
    }

    public override void Update()
    {
        //Cache Snapshot
        var snapshot = _peersSnapshot;
        if (_wsServer == null) return;
        foreach (var peer in snapshot.Cast<McWssMcApiNetPeer>()) UpdatePeer(peer);
    }

    public override void Stop()
    {
        lock (_lock)
        {
            var snapshot = _peersSnapshot;
            if (_wsServer == null)
            {
                ClearMcWssPeers();
                return;
            }

            try
            {
                _wsServer.Dispose();
            }
            catch
            {
                //Do Nothing
            }

            ClearMcWssPeers();
            foreach (var client in snapshot)
                try
                {
                    Disconnect(client, true);
                }
                catch
                {
                    //Do Nothing
                }

            _wsServer = null;
        }
    }

    public override void SendPacket<T>(McApiNetPeer netPeer, T packet)
    {
        if (_wsServer == null ||
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

            netPeer.OutgoingQueue.Enqueue(new McApiNetPeer.QueuedPacket(_writer.CopyData(), string.Empty));
        }
    }

    public override void Broadcast<T>(T packet, params McApiNetPeer?[] excludes)
    {
        if (_wsServer == null || Config.DisabledPacketTypes.Contains(packet.PacketType)) return;
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
        if (netPeer.Server != this || netPeer is not McWssMcApiNetPeer mcWssNetPeer) return; //Not our client.
        if (netPeer.ConnectionState is McApiConnectionState.Disconnected or McApiConnectionState.Disconnecting)
        {
            //Already disconnected or disconnecting, we can just force closure of the client.
            //The original disconnection call or thread will raise the event.
            if (!force) return;
            TryRemoveMcWssPeer(mcWssNetPeer.Connection, out _);
            CloseClient(mcWssNetPeer.Connection);
            return;
        }

        var wasConnected = mcWssNetPeer.ConnectionState == McApiConnectionState.Connected;
        mcWssNetPeer.ConnectionState = McApiConnectionState.Disconnecting;
        var sessionToken = mcWssNetPeer.SessionToken;
        var logoutPacket = PacketPool<McApiLogoutRequestPacket>.GetPacket(() => new McApiLogoutRequestPacket());
        try
        {
            if (force)
            {
                TryRemoveMcWssPeer(mcWssNetPeer.Connection, out _);
                CloseClient(mcWssNetPeer.Connection);
                return;
            }

            logoutPacket.Set(netPeer.SessionToken);
            SendPacket(netPeer, logoutPacket);
        }
        finally
        {
            logoutPacket.Return();
            mcWssNetPeer.SetSessionToken(string.Empty);
            mcWssNetPeer.ConnectionState = McApiConnectionState.Disconnected;
            if (wasConnected)
                OnPeerDisconnected?.Invoke(mcWssNetPeer, sessionToken);
        }
    }

    protected override void AcceptRequest(McApiLoginRequestPacket packet, McApiNetPeer netPeer)
    {
        if (netPeer is not McWssMcApiNetPeer mcWssNetPeer) return;
        var acceptPacket = PacketPool<McApiAcceptResponsePacket>.GetPacket(() => new McApiAcceptResponsePacket());
        try
        {
            if (mcWssNetPeer.ConnectionState != McApiConnectionState.Connected)
                mcWssNetPeer.SetSessionToken(Guid.NewGuid().ToString());

            acceptPacket.Set(packet.RequestId, mcWssNetPeer.SessionToken);
            SendPacket(mcWssNetPeer, acceptPacket);

            mcWssNetPeer.ConnectionState = McApiConnectionState.Connected;
            OnPeerConnected?.Invoke(mcWssNetPeer, mcWssNetPeer.SessionToken);
        }
        catch
        {
            RejectRequest(packet, "McApi.DisconnectReason.Error", mcWssNetPeer); //Auth flow is a bit different here.
        }
        finally
        {
            acceptPacket.Return();
        }
    }

    protected override void RejectRequest(McApiLoginRequestPacket packet, string reason, McApiNetPeer netPeer)
    {
        if (netPeer is not McWssMcApiNetPeer mcWssNetPeer) return;
        var denyPacket = PacketPool<McApiDenyResponsePacket>.GetPacket(() => new McApiDenyResponsePacket());
        try
        {
            denyPacket.Set(packet.RequestId, reason);
            SendPacket(mcWssNetPeer, denyPacket);
        }
        finally
        {
            denyPacket.Return();
            mcWssNetPeer.SetSessionToken(string.Empty);
            mcWssNetPeer.ConnectionState = McApiConnectionState.Disconnected;
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

    private void HandleDataTunnelCommandResponse(IWebSocketConnection socket, string data)
    {
        try
        {
            if (!TryGetMcWssPeer(socket, out var peer) || string.IsNullOrWhiteSpace(data)) return;
            var packedPackets = Z85.GetBytesWithPadding(data);
            lock (_mcWssReader)
            {
                _mcWssReader.Clear();
                _mcWssReader.SetSource(packedPackets);
                while (!_mcWssReader.EndOfData)
                {
                    var packetSize = _mcWssReader.GetUShort();
                    if (packetSize <= 0) continue;
                    if (_mcWssReader.AvailableBytes < packetSize)
                        return;

                    var packet = new byte[packetSize];
                    _mcWssReader.GetBytes(packet, packetSize);
                    peer.IncomingQueue.Enqueue(new McApiNetPeer.QueuedPacket(packet, string.Empty));
                }
            }
        }
        catch
        {
            //Do Nothing
        }
    }

    private void UpdatePeer(McWssMcApiNetPeer mcWssNetPeer)
    {
        var connection = mcWssNetPeer.Connection;
        ProcessPackets(mcWssNetPeer);

        for (var i = 0; i < Config.CommandsPerTick; i++)
            if (!SendPacketsLogic(connection, mcWssNetPeer))
                SendPacketCommand(connection, string.Empty);

        if (DateTime.UtcNow - mcWssNetPeer.LastUpdate < TimeSpan.FromMilliseconds(Config.MaxTimeoutMs)) return;
        Disconnect(mcWssNetPeer);
        //Double the amount of time. We remove the peer.
        if (DateTime.UtcNow - mcWssNetPeer.LastUpdate < TimeSpan.FromMilliseconds(Config.MaxTimeoutMs * 2)) return;
        Disconnect(mcWssNetPeer, true);
    }

    private void ProcessPackets(McWssMcApiNetPeer mcWssNetPeer)
    {
        lock (_reader)
        {
            while (mcWssNetPeer.IncomingQueue.TryDequeue(out var packet))
                try
                {
                    _reader.Clear();
                    _reader.SetSource(packet.Data);
                    ProcessPacket(_reader, mcApiPacket =>
                    {
                        mcWssNetPeer.LastUpdate = DateTime.UtcNow;
                        if (!AuthorizePacket(mcApiPacket, mcWssNetPeer, mcWssNetPeer.SessionToken) ||
                            Config.DisabledPacketTypes.Contains(mcApiPacket.PacketType)) return;
                        ExecutePacket(mcApiPacket, mcWssNetPeer);
                    });
                }
                catch
                {
                    //Do Nothing
                }
        }
    }

    private bool SendPacketsLogic(IWebSocketConnection socket, McApiNetPeer netPeer)
    {
        if (!netPeer.OutgoingQueue.TryDequeue(out var outboundPacket)) return false;
        string data;
        lock (_mcWssWriter)
        {
            _mcWssWriter.Reset();
            _mcWssWriter.Put((ushort)outboundPacket.Data.Length);
            _mcWssWriter.Put(outboundPacket.Data);

            while (netPeer.OutgoingQueue.TryPeek(out var nextPacket) &&
                   _mcWssWriter.Length + sizeof(ushort) + nextPacket.Data.Length <= Config.MaxByteLengthPerCommand &&
                   netPeer.OutgoingQueue.TryDequeue(out outboundPacket))
            {
                _mcWssWriter.Put((ushort)outboundPacket.Data.Length);
                _mcWssWriter.Put(outboundPacket.Data);
            }

            data = Z85.GetStringWithPadding(_mcWssWriter.AsReadOnlySpan());
        }

        SendPacketCommand(socket, data);
        return true;
    }

    private void SendPacketCommand(IWebSocketConnection socket, string packetData)
    {
        var packet =
            new McWssCommandRequest(
                $"{Config.DataTunnelCommand} {Config.MaxByteLengthPerCommand} \"{packetData}\"");
        socket.Send(JsonSerializer.Serialize(packet, McWssCommandRequestGenerationContext.Default.McWssCommandRequest));
    }

    #region Websocket Events

    private void OnClientConnected(IWebSocketConnection socket)
    {
        var count = _peersSnapshot.Count;
        if (count >= Config.MaxClients)
        {
            CloseClient(socket, 1013); //Full.
            return;
        }

        var netPeer = new McWssMcApiNetPeer(this, socket);
        if (!TryAddMcWssPeer(socket, netPeer))
            CloseClient(socket, 1011); //Error
    }

    private void OnClientDisconnected(IWebSocketConnection socket)
    {
        if (!TryRemoveMcWssPeer(socket, out var mcApiPeer))
        {
            CloseClient(socket, 1013);
            return;
        }

        var wasConnected = mcApiPeer.ConnectionState != McApiConnectionState.Disconnected;
        var sessionToken = mcApiPeer.SessionToken;
        mcApiPeer.SetSessionToken(string.Empty);
        mcApiPeer.ConnectionState = McApiConnectionState.Disconnected;
        if (wasConnected)
            OnPeerDisconnected?.Invoke(mcApiPeer, sessionToken);
    }

    private void OnMessageReceived(IWebSocketConnection socket, string message)
    {
        try
        {
            var genericPacket = JsonSerializer.Deserialize<McWssGenericPacket>(message,
                McWssGenericPacketGenerationContext.Default.McWssGenericPacket);
            if (genericPacket == null || genericPacket.header.messagePurpose != "commandResponse") return;

            var commandResponsePacket = JsonSerializer.Deserialize<McWssCommandResponse>(message,
                McWssCommandResponseGenerationContext.Default.McWssCommandResponse);
            if (commandResponsePacket is not { StatusCode: 0 }) return;
            HandleDataTunnelCommandResponse(socket, commandResponsePacket.body.statusMessage);
        }
        catch
        {
            //Do Nothing.
        }
    }

    #endregion

    private bool TryAddMcWssPeer(IWebSocketConnection connection, McWssMcApiNetPeer peer)
    {
        lock (_lock)
        {
            if (!_mcApiPeers.TryAdd(connection, peer)) return false;
            _peersSnapshot = [.._mcApiPeers.Values];
            return true;
        }
    }

    private bool TryRemoveMcWssPeer(IWebSocketConnection connection, [NotNullWhen(true)] out McWssMcApiNetPeer? peer)
    {
        lock (_lock)
        {
            if (!_mcApiPeers.Remove(connection, out peer)) return false;
            _peersSnapshot = [.._mcApiPeers.Values];
            return true;
        }
    }

    private bool TryGetMcWssPeer(IWebSocketConnection connection, [NotNullWhen(true)] out McWssMcApiNetPeer? peer)
    {
        lock (_lock)
        {
            return _mcApiPeers.TryGetValue(connection, out peer);
        }
    }

    private void ClearMcWssPeers()
    {
        lock (_lock)
        {
            _mcApiPeers.Clear();
            _peersSnapshot = ImmutableList<McApiNetPeer>.Empty;
        }
    }

    private static void CloseClient(IWebSocketConnection connection)
    {
        try
        {
            if (connection.IsAvailable)
                connection.Close();
        }
        catch
        {
            //Do Nothing
        }
    }

    private static void CloseClient(IWebSocketConnection connection, int errorCode)
    {
        try
        {
            if (connection.IsAvailable)
                connection.Close(errorCode);
        }
        catch
        {
            //Do Nothing
        }
    }

    public class McWssMcApiConfig
    {
        [JsonConverter(typeof(JsonBooleanConverter))]
        public bool Enabled { get; set; }

        public string LoginToken { get; set; } = Guid.NewGuid().ToString();
        public string Hostname { get; set; } = "ws://127.0.0.1:9051/";
        public uint ExternalPort { get; set; }
        public uint PortMappingLifetimeMinutes { get; set; } = 60;
        public uint PortMappingTimeoutSeconds { get; set; } = 5;
        public uint MaxClients { get; set; } = 1;
        public uint MaxTimeoutMs { get; set; } = 10000;

        [JsonConverter(typeof(JsonBooleanConverter))]
        public bool AutoOpenPort { get; set; } = true;

        public string DataTunnelCommand { get; set; } = "voicecraft:data_tunnel";
        public uint CommandsPerTick { get; set; } = 3;
        public uint MaxByteLengthPerCommand { get; set; } = 300;
        public HashSet<McApiPacketType> DisabledPacketTypes { get; set; } = [];
    }
}