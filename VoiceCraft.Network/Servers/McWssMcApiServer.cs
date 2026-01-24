using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
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
    private readonly ConcurrentDictionary<IWebSocketConnection, McWssMcApiNetPeer> _mcApiPeers = new();
    private readonly NetDataReader _reader = new();
    private readonly NetDataWriter _writer = new();
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

    public override string LoginToken => _config.LoginToken;
    public override uint MaxClients => _config.MaxClients;

    public override int ConnectedPeers =>
        _mcApiPeers.Count(x => x.Value.ConnectionState == McApiConnectionState.Connected);

    public override event Action<McApiNetPeer, string>? OnPeerConnected;
    public override event Action<McApiNetPeer, string>? OnPeerDisconnected;

    public override void Start()
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

    public override void Update()
    {
        if (_wsServer == null) return;
        foreach (var peer in _mcApiPeers) UpdatePeer(peer.Key, peer.Value);
    }

    public override void Stop()
    {
        if (_wsServer == null) return;
        _wsServer.Dispose();
        foreach (var client in _mcApiPeers)
            try
            {
                client.Key.Close();
            }
            catch
            {
                //Do Nothing
            }

        _wsServer = null;
    }

    public override void SendPacket<T>(McApiNetPeer netPeer, T packet)
    {
        if (_wsServer == null || netPeer.ConnectionState != McApiConnectionState.Connected ||
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
        if (_wsServer == null || Config.DisabledPacketTypes.Contains(packet.PacketType)) return;
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
        if (netPeer is not McWssMcApiNetPeer { ConnectionState: McApiConnectionState.Connected } mcWssNetPeer) return;
        var logoutPacket = PacketPool<McApiLogoutRequestPacket>.GetPacket(() => new McApiLogoutRequestPacket())
            .Set(netPeer.SessionToken);
        try
        {
            var sessionToken = mcWssNetPeer.SessionToken;
            mcWssNetPeer.SetConnectionState(McApiConnectionState.Disconnected);
            mcWssNetPeer.SetSessionToken(string.Empty);
            OnPeerDisconnected?.Invoke(mcWssNetPeer, sessionToken);
            if (force)
            {
                _mcApiPeers.TryRemove(mcWssNetPeer.Connection, out _); //Remove Immediately.
                mcWssNetPeer.Connection.Close(); //Close the connection.
                return;
            }

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
        if (data is not McWssMcApiNetPeer mcWssNetPeer) return;
        try
        {
            if (mcWssNetPeer.ConnectionState != McApiConnectionState.Connected)
            {
                mcWssNetPeer.SetSessionToken(Guid.NewGuid().ToString());
                mcWssNetPeer.SetConnectionState(McApiConnectionState.Connected);
            }

            SendPacket(mcWssNetPeer,
                PacketPool<McApiAcceptResponsePacket>.GetPacket(() => new McApiAcceptResponsePacket())
                    .Set(packet.RequestId, mcWssNetPeer.SessionToken));
            OnPeerConnected?.Invoke(mcWssNetPeer, mcWssNetPeer.SessionToken);
        }
        catch
        {
            RejectRequest(packet, "McApi.DisconnectReason.Error", mcWssNetPeer); //Auth flow is a bit different here.
        }
    }

    protected override void RejectRequest(McApiLoginRequestPacket packet, string reason, object? data)
    {
        if (data is not McWssMcApiNetPeer mcWssNetPeer) return;
        var responsePacket = PacketPool<McApiDenyResponsePacket>.GetPacket(() => new McApiDenyResponsePacket())
            .Set(packet.RequestId, reason);
        try
        {
            mcWssNetPeer.SetSessionToken("");
            mcWssNetPeer.SetConnectionState(McApiConnectionState.Disconnected);
            lock (_writer)
            {
                _writer.Reset();
                _writer.Put((byte)responsePacket.PacketType);
                _writer.Put(responsePacket);
                if (_writer.Length > short.MaxValue)
                    throw new ArgumentOutOfRangeException(nameof(packet));

                var encodedPacket = Z85.GetStringWithPadding(_writer.AsReadOnlySpan());
                mcWssNetPeer.OutgoingQueue.Enqueue(new McApiNetPeer.QueuedPacket(encodedPacket, string.Empty));
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

    private void HandleDataTunnelCommandResponse(IWebSocketConnection socket, string data)
    {
        try
        {
            if (!_mcApiPeers.TryGetValue(socket, out var peer) || string.IsNullOrWhiteSpace(data)) return;
            var packets = data.Split("|");

            foreach (var packet in packets)
                peer.IncomingQueue.Enqueue(new McApiNetPeer.QueuedPacket(packet, string.Empty));
        }
        catch
        {
            //Do Nothing
        }
    }

    private void UpdatePeer(IWebSocketConnection connection, McWssMcApiNetPeer mcWssNetPeer)
    {
        lock (_reader)
        {
            while (mcWssNetPeer.IncomingQueue.TryDequeue(out var packet))
                try
                {
                    _reader.Clear();
                    _reader.SetSource(Z85.GetBytesWithPadding(packet.Data));
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

        for (var i = 0; i < Config.CommandsPerTick; i++)
            if (!SendPacketsLogic(connection, mcWssNetPeer))
                SendPacketCommand(connection, string.Empty);

        if (DateTime.UtcNow - mcWssNetPeer.LastUpdate < TimeSpan.FromMilliseconds(Config.MaxTimeoutMs)) return;
        Disconnect(mcWssNetPeer);
        //Double the amount of time. We remove the peer.
        if (DateTime.UtcNow - mcWssNetPeer.LastUpdate < TimeSpan.FromMilliseconds(Config.MaxTimeoutMs * 2)) return;
        if (_mcApiPeers.TryRemove(connection, out _))
            connection.Close();
    }

    private bool SendPacketsLogic(IWebSocketConnection socket, McApiNetPeer netPeer)
    {
        var stringBuilder = new StringBuilder();
        if (!netPeer.OutgoingQueue.TryDequeue(out var outboundPacket)) return false;
        stringBuilder.Append(outboundPacket);
        while (netPeer.OutgoingQueue.TryDequeue(out outboundPacket) &&
               stringBuilder.Length < Config.MaxStringLengthPerCommand)
            stringBuilder.Append($"|{outboundPacket}");

        SendPacketCommand(socket, stringBuilder.ToString());
        return true;
    }

    private void SendPacketCommand(IWebSocketConnection socket, string packetData)
    {
        var packet =
            new McWssCommandRequest($"{Config.DataTunnelCommand} {Config.MaxStringLengthPerCommand} \"{packetData}\"");
        socket.Send(JsonSerializer.Serialize(packet, McWssCommandRequestGenerationContext.Default.McWssCommandRequest));
    }

    #region Websocket Events

    private void OnClientConnected(IWebSocketConnection socket)
    {
        if (_mcApiPeers.Count >= Config.MaxClients)
            socket.Close(); //Full.

        var netPeer = new McWssMcApiNetPeer(socket);
        _mcApiPeers.TryAdd(socket, netPeer);
    }

    private void OnClientDisconnected(IWebSocketConnection socket)
    {
        if (!_mcApiPeers.TryRemove(socket, out var mcApiPeer)) return;
        if (mcApiPeer.ConnectionState != McApiConnectionState.Connected) return;
        var sessionToken = mcApiPeer.SessionToken;
        mcApiPeer.SetConnectionState(McApiConnectionState.Disconnected);
        mcApiPeer.SetSessionToken(string.Empty);
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

    public class McWssMcApiConfig
    {
        [JsonConverter(typeof(JsonBooleanConverter))]
        public bool Enabled { get; set; }

        public string LoginToken { get; set; } = Guid.NewGuid().ToString();
        public string Hostname { get; set; } = "ws://127.0.0.1:9051/";
        public uint MaxClients { get; set; } = 1;
        public uint MaxTimeoutMs { get; set; } = 10000;
        public string DataTunnelCommand { get; set; } = "voicecraft:data_tunnel";
        public uint CommandsPerTick { get; set; } = 5;
        public uint MaxStringLengthPerCommand { get; set; } = 1000;
        public HashSet<McApiPacketType> DisabledPacketTypes { get; set; } = [];
    }
}