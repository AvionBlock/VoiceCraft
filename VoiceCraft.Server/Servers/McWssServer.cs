using System.Collections.Concurrent;
using System.Text.Json;
using LiteNetLib.Utils;
using Spectre.Console;
using VoiceCraft.Core;
using VoiceCraft.Core.Network;
using VoiceCraft.Core.Network.McApiPackets;
using VoiceCraft.Core.Network.McWssPackets;
using VoiceCraft.Server.Config;
using Fleck;
using VoiceCraft.Core.Network.McApiPackets.Request;
using VoiceCraft.Core.Network.McApiPackets.Response;
using VoiceCraft.Core.World;

namespace VoiceCraft.Server.Servers;

public class McWssServer(VoiceCraftWorld world)
{
    private static readonly Version McWssVersion = new(Constants.Minor, Constants.Major, 0);

    private readonly ConcurrentDictionary<IWebSocketConnection, McApiNetPeer> _mcApiPeers = [];
    private readonly NetDataReader _reader = new();
    private readonly NetDataWriter _writer = new();
    private readonly VoiceCraftWorld _world = world;
    private WebSocketServer? _wsServer;

    //Public Properties
    public McWssConfig Config { get; private set; } = new();

    //Events
    public event Action<McApiNetPeer>? OnPeerConnected;
    public event Action<McApiNetPeer>? OnPeerDisconnected;

    public void Start(McWssConfig? config = null)
    {
        Stop();

        if (config != null)
            Config = config;

        try
        {
            AnsiConsole.WriteLine(Locales.Locales.McWssServer_Starting);
            _wsServer = new WebSocketServer(Config.Hostname);

            _wsServer.Start(socket =>
            {
                socket.OnOpen = () => OnClientConnected(socket);
                socket.OnClose = () => OnClientDisconnected(socket);
                socket.OnMessage = message => OnMessageReceived(socket, message);
            });
            AnsiConsole.MarkupLine($"[green]{Locales.Locales.McWssServer_Success}[/]");
        }
        catch (Exception ex)
        {
            throw new Exception(Locales.Locales.McWssServer_Exceptions_Failed, ex);
        }
    }

    public void Update()
    {
        foreach (var peer in _mcApiPeers) UpdatePeer(peer);
    }

    public void Stop()
    {
        if (_wsServer == null) return;
        AnsiConsole.WriteLine(Locales.Locales.McWssServer_Stopping);
        foreach (var client in _mcApiPeers)
        {
            try
            {
                client.Key.Close();
            }
            catch (Exception ex)
            {
                AnsiConsole.WriteException(ex);
            }
        }

        _wsServer.Dispose();
        _wsServer = null;
        AnsiConsole.MarkupLine($"[green]{Locales.Locales.McWssServer_Stopped}[/]");
    }

    public void SendPacket<T>(McApiNetPeer netPeer, T packet) where T : IMcApiPacket
    {
        try
        {
            lock (_writer)
            {
                _writer.Reset();
                _writer.Put((byte)packet.PacketType);
                packet.Serialize(_writer);
                netPeer.SendPacket(_writer);
            }
        }
        finally
        {
            PacketPool<T>.Return(packet);
        }
    }

    public void Broadcast<T>(T packet, params McApiNetPeer?[] excludes) where T : IMcApiPacket
    {
        try
        {
            lock (_writer)
            {
                var netPeers = _mcApiPeers.Where(x => x.Value.Connected);
                _writer.Reset();
                _writer.Put((byte)packet.PacketType);
                packet.Serialize(_writer);
                foreach (var netPeer in netPeers)
                {
                    if (excludes.Contains(netPeer.Value)) continue;
                    netPeer.Value.SendPacket(_writer);
                }
            }
        }
        finally
        {
            PacketPool<T>.Return(packet);
        }
    }

    private void SendPacket<T>(IWebSocketConnection socket, T packet) where T : IMcApiPacket
    {
        try
        {
            lock (_writer)
            {
                _writer.Reset();
                _writer.Put((byte)packet.PacketType);
                packet.Serialize(_writer);
                SendPacket(socket, Z85.GetStringWithPadding(_writer.CopyData()));
            }
        }
        finally
        {
            PacketPool<T>.Return(packet);
        }
    }

    private void SendPacket(IWebSocketConnection socket, string packetData)
    {
        var packet = new McWssCommandRequest($"{Config.TunnelCommand} \"{packetData}\"");
        socket.Send(JsonSerializer.Serialize(packet));
    }

    private void UpdatePeer(KeyValuePair<IWebSocketConnection, McApiNetPeer> peer)
    {
        lock (_reader)
        {
            while (peer.Value.RetrieveInboundPacket(out var packetData))
                try
                {
                    _reader.Clear();
                    _reader.SetSource(packetData);
                    var packetType = _reader.GetByte();
                    var pt = (McApiPacketType)packetType;
                    HandlePacket(pt, _reader, peer.Key, peer.Value);
                }
                catch
                {
                    //Do Nothing
                }
        }

        var sent = false;
        while (peer.Value.RetrieveOutboundPacket(out var outboundPacket))
        {
            sent = true;
            SendPacket(peer.Key, Z85.GetStringWithPadding(outboundPacket));
        }

        if (!sent)
            SendPacket(peer.Key, string.Empty);

        switch (peer.Value.Connected)
        {
            case true when DateTime.UtcNow - peer.Value.LastPing >= TimeSpan.FromMilliseconds(Config.MaxTimeoutMs):
                peer.Value.Disconnect();
                break;
        }
    }

    private void OnClientConnected(IWebSocketConnection socket)
    {
        if (_mcApiPeers.Count >= Config.MaxClients)
            socket.Close(); //Full.

        var netPeer = new McApiNetPeer();
        _mcApiPeers.TryAdd(socket, netPeer);
        netPeer.OnConnected += McApiNetPeerOnConnected;
        netPeer.OnDisconnected += McApiNetPeerOnDisconnected;
    }

    private void OnClientDisconnected(IWebSocketConnection socket)
    {
        if (!_mcApiPeers.TryRemove(socket, out var netPeer)) return;
        netPeer.Disconnect();
        netPeer.OnConnected -= McApiNetPeerOnConnected;
        netPeer.OnDisconnected -= McApiNetPeerOnDisconnected;
    }

    private void OnMessageReceived(IWebSocketConnection socket, string message)
    {
        try
        {
            var genericPacket = JsonSerializer.Deserialize<McWssGenericPacket>(message);
            if (genericPacket == null) return;

            switch (genericPacket.header.messagePurpose)
            {
                case "commandResponse":
                    var commandResponsePacket = JsonSerializer.Deserialize<McWssCommandResponse>(message);
                    if (commandResponsePacket != null && _mcApiPeers.TryGetValue(socket, out var peer) &&
                        !string.IsNullOrWhiteSpace(commandResponsePacket.StatusMessage) &&
                        commandResponsePacket.StatusCode == 0)
                    {
                        var packets = commandResponsePacket.StatusMessage.Split("|");
                        foreach (var packet in packets)
                        {
                            try
                            {
                                peer.ReceiveInboundPacket(Z85.GetBytesWithPadding(packet));
                            }
                            catch
                            {
                                //Ignored
                            }
                        }
                    }

                    break;
            }
        }
        catch
        {
            //Ignored
        }
    }

    private void HandlePacket(McApiPacketType packetType, NetDataReader reader, IWebSocketConnection socket,
        McApiNetPeer peer)
    {
        if (packetType == McApiPacketType.LoginRequest)
        {
            var loginRequestPacket = PacketPool<McApiLoginRequestPacket>.GetPacket();
            loginRequestPacket.Deserialize(reader);
            HandleLoginRequestPacket(loginRequestPacket, socket, peer);
            return;
        }

        if (!peer.Connected) return;

        switch (packetType)
        {
            case McApiPacketType.LogoutRequest:
                var logoutRequestPacket = PacketPool<McApiLogoutRequestPacket>.GetPacket();
                logoutRequestPacket.Deserialize(reader);
                HandleLogoutRequestPacket(logoutRequestPacket, peer);
                break;
            case McApiPacketType.PingRequest:
                var pingRequestPacket = PacketPool<McApiPingRequestPacket>.GetPacket();
                pingRequestPacket.Deserialize(reader);
                HandlePingRequestPacket(pingRequestPacket, peer);
                break;
            case McApiPacketType.SetEntityTitleRequest:
                var setEntityTitleRequestPacket = PacketPool<McApiSetEntityTitleRequestPacket>.GetPacket();
                setEntityTitleRequestPacket.Deserialize(reader);
                HandleSetEntityTitleRequestPacket(setEntityTitleRequestPacket, peer);
                break;
            case McApiPacketType.SetEntityDescriptionRequest:
                var setEntityDescriptionRequestPacket =
                    PacketPool<McApiSetEntityDescriptionRequestPacket>.GetPacket();
                setEntityDescriptionRequestPacket.Deserialize(reader);
                HandleSetEntityDescriptionRequestPacket(setEntityDescriptionRequestPacket, peer);
                break;
            case McApiPacketType.SetEntityWorldIdRequest:
                var setEntityWorldIdRequestPacket = PacketPool<McApiSetEntityWorldIdRequestPacket>.GetPacket();
                setEntityWorldIdRequestPacket.Deserialize(reader);
                HandleSetEntityWorldIdRequestPacket(setEntityWorldIdRequestPacket, peer);
                break;
            case McApiPacketType.SetEntityNameRequest:
                var setEntityNameRequestPacket = PacketPool<McApiSetEntityNameRequestPacket>.GetPacket();
                setEntityNameRequestPacket.Deserialize(reader);
                HandleSetEntityNameRequestPacket(setEntityNameRequestPacket, peer);
                break;
            case McApiPacketType.SetEntityTalkBitmaskRequest:
                var setEntityTalkBitmaskRequestPacket = PacketPool<McApiSetEntityTalkBitmaskRequestPacket>.GetPacket();
                setEntityTalkBitmaskRequestPacket.Deserialize(reader);
                HandleSetEntityTalkBitmaskRequestPacket(setEntityTalkBitmaskRequestPacket, peer);
                break;
            case McApiPacketType.SetEntityListenBitmaskRequest:
                var setEntityListenBitmaskRequestPacket =
                    PacketPool<McApiSetEntityListenBitmaskRequestPacket>.GetPacket();
                setEntityListenBitmaskRequestPacket.Deserialize(reader);
                HandleSetEntityListenBitmaskRequestPacket(setEntityListenBitmaskRequestPacket, peer);
                break;
            case McApiPacketType.SetEntityEffectBitmaskRequest:
                var setEntityEffectBitmaskRequestPacket =
                    PacketPool<McApiSetEntityEffectBitmaskRequestPacket>.GetPacket();
                setEntityEffectBitmaskRequestPacket.Deserialize(reader);
                HandleSetEntityEffectBitmaskRequestPacket(setEntityEffectBitmaskRequestPacket, peer);
                break;
            case McApiPacketType.SetEntityPositionRequest:
                var setEntityPositionRequestPacket = PacketPool<McApiSetEntityPositionRequestPacket>.GetPacket();
                setEntityPositionRequestPacket.Deserialize(reader);
                HandleSetEntityPositionRequestPacket(setEntityPositionRequestPacket, peer);
                break;
            case McApiPacketType.SetEntityRotationRequest:
                var setEntityRotationRequestPacket = PacketPool<McApiSetEntityRotationRequestPacket>.GetPacket();
                setEntityRotationRequestPacket.Deserialize(reader);
                HandleSetEntityRotationRequestPacket(setEntityRotationRequestPacket, peer);
                break;
            case McApiPacketType.SetEntityCaveFactorRequest:
                var setEntityCaveFactorRequestPacket = PacketPool<McApiSetEntityCaveFactorRequestPacket>.GetPacket();
                setEntityCaveFactorRequestPacket.Deserialize(reader);
                HandleSetEntityCaveFactorRequestPacket(setEntityCaveFactorRequestPacket, peer);
                break;
            case McApiPacketType.SetEntityMuffleFactorRequest:
                var setEntityMuffleFactorRequestPacket =
                    PacketPool<McApiSetEntityMuffleFactorRequestPacket>.GetPacket();
                setEntityMuffleFactorRequestPacket.Deserialize(reader);
                HandleSetEntityMuffleFactorRequestPacket(setEntityMuffleFactorRequestPacket, peer);
                break;
        }
    }

    private void McApiNetPeerOnConnected(McApiNetPeer peer)
    {
        OnPeerConnected?.Invoke(peer);
    }

    private void McApiNetPeerOnDisconnected(McApiNetPeer peer)
    {
        OnPeerDisconnected?.Invoke(peer);
    }

    private void HandleLoginRequestPacket(McApiLoginRequestPacket packet, IWebSocketConnection socket,
        McApiNetPeer netPeer)
    {
        try
        {
            if (netPeer.Connected)
            {
                SendPacket(netPeer,
                    PacketPool<McApiAcceptResponsePacket>.GetPacket().Set(packet.RequestId, netPeer.Token));
                OnPeerConnected?.Invoke(netPeer);
                return;
            }

            if (!string.IsNullOrEmpty(Config.LoginToken) && Config.LoginToken != packet.Token)
            {
                SendPacket(socket, PacketPool<McApiDenyResponsePacket>.GetPacket().Set(packet.RequestId, packet.Token,
                    "VcMcApi.DisconnectReason.InvalidLoginToken"));
                return;
            }

            if (packet.Version.Major != McWssVersion.Major || packet.Version.Minor != McWssVersion.Minor)
            {
                SendPacket(socket, PacketPool<McApiDenyResponsePacket>.GetPacket().Set(packet.RequestId, packet.Token,
                    "VcMcApi.DisconnectReason.IncompatibleVersion"));
                return;
            }

            netPeer.AcceptConnection(Guid.NewGuid().ToString());
            SendPacket(netPeer, PacketPool<McApiAcceptResponsePacket>.GetPacket().Set(packet.RequestId, netPeer.Token));
        }
        finally
        {
            PacketPool<McApiLoginRequestPacket>.Return(packet);
        }
    }

    private static void HandleLogoutRequestPacket(McApiLogoutRequestPacket packet, McApiNetPeer netPeer)
    {
        try
        {
            if (netPeer.Token != packet.Token) return;
            netPeer.Disconnect();
        }
        finally
        {
            PacketPool<McApiLogoutRequestPacket>.Return(packet);
        }
    }

    private void HandlePingRequestPacket(McApiPingRequestPacket packet, McApiNetPeer netPeer)
    {
        try
        {
            if (netPeer.Token != packet.Token) return; //Needs a session token at least.
            SendPacket(netPeer, PacketPool<McApiPingResponsePacket>.GetPacket().Set(packet.Token));
        }
        finally
        {
            PacketPool<McApiPingRequestPacket>.Return(packet);
        }
    }

    private void HandleSetEntityTitleRequestPacket(McApiSetEntityTitleRequestPacket packet, McApiNetPeer netPeer)
    {
        try
        {
            if (netPeer.Token != packet.Token) return; //Needs a session token at least.
            var entity = _world.GetEntity(packet.Id);
            if (entity is not VoiceCraftNetworkEntity networkEntity) return;
            networkEntity.SetTitle(packet.Value);
        }
        finally
        {
            PacketPool<McApiSetEntityTitleRequestPacket>.Return(packet);
        }
    }

    private void HandleSetEntityDescriptionRequestPacket(McApiSetEntityDescriptionRequestPacket packet,
        McApiNetPeer netPeer)
    {
        try
        {
            if (netPeer.Token != packet.Token) return; //Needs a session token at least.
            var entity = _world.GetEntity(packet.Id);
            if (entity is not VoiceCraftNetworkEntity networkEntity) return;
            networkEntity.SetDescription(packet.Value);
        }
        finally
        {
            PacketPool<McApiSetEntityDescriptionRequestPacket>.Return(packet);
        }
    }

    private void HandleSetEntityWorldIdRequestPacket(McApiSetEntityWorldIdRequestPacket packet,
        McApiNetPeer netPeer)
    {
        try
        {
            if (netPeer.Token != packet.Token) return; //Needs a session token at least.
            var entity = _world.GetEntity(packet.Id);
            if (entity == null) return;
            entity.WorldId = packet.Value;
        }
        finally
        {
            PacketPool<McApiSetEntityWorldIdRequestPacket>.Return(packet);
        }
    }

    private void HandleSetEntityNameRequestPacket(McApiSetEntityNameRequestPacket packet,
        McApiNetPeer netPeer)
    {
        try
        {
            if (netPeer.Token != packet.Token) return; //Needs a session token at least.
            var entity = _world.GetEntity(packet.Id);
            if (entity == null) return;
            entity.Name = packet.Value;
        }
        finally
        {
            PacketPool<McApiSetEntityNameRequestPacket>.Return(packet);
        }
    }

    private void HandleSetEntityTalkBitmaskRequestPacket(McApiSetEntityTalkBitmaskRequestPacket packet,
        McApiNetPeer netPeer)
    {
        try
        {
            if (netPeer.Token != packet.Token) return; //Needs a session token at least.
            var entity = _world.GetEntity(packet.Id);
            if (entity == null) return;
            entity.TalkBitmask = packet.Value;
        }
        finally
        {
            PacketPool<McApiSetEntityTalkBitmaskRequestPacket>.Return(packet);
        }
    }

    private void HandleSetEntityListenBitmaskRequestPacket(McApiSetEntityListenBitmaskRequestPacket packet,
        McApiNetPeer netPeer)
    {
        try
        {
            if (netPeer.Token != packet.Token) return; //Needs a session token at least.
            var entity = _world.GetEntity(packet.Id);
            if (entity == null) return;
            entity.ListenBitmask = packet.Value;
        }
        finally
        {
            PacketPool<McApiSetEntityListenBitmaskRequestPacket>.Return(packet);
        }
    }

    private void HandleSetEntityEffectBitmaskRequestPacket(McApiSetEntityEffectBitmaskRequestPacket packet,
        McApiNetPeer netPeer)
    {
        try
        {
            if (netPeer.Token != packet.Token) return; //Needs a session token at least.
            var entity = _world.GetEntity(packet.Id);
            if (entity == null) return;
            entity.EffectBitmask = packet.Value;
        }
        finally
        {
            PacketPool<McApiSetEntityEffectBitmaskRequestPacket>.Return(packet);
        }
    }

    private void HandleSetEntityPositionRequestPacket(McApiSetEntityPositionRequestPacket packet,
        McApiNetPeer netPeer)
    {
        try
        {
            if (netPeer.Token != packet.Token) return; //Needs a session token at least.
            var entity = _world.GetEntity(packet.Id);
            if (entity == null) return;
            entity.Position = packet.Value;
        }
        finally
        {
            PacketPool<McApiSetEntityPositionRequestPacket>.Return(packet);
        }
    }

    private void HandleSetEntityRotationRequestPacket(McApiSetEntityRotationRequestPacket packet,
        McApiNetPeer netPeer)
    {
        try
        {
            if (netPeer.Token != packet.Token) return; //Needs a session token at least.
            var entity = _world.GetEntity(packet.Id);
            if (entity == null) return;
            entity.Rotation = packet.Value;
        }
        finally
        {
            PacketPool<McApiSetEntityRotationRequestPacket>.Return(packet);
        }
    }

    private void HandleSetEntityCaveFactorRequestPacket(McApiSetEntityCaveFactorRequestPacket packet,
        McApiNetPeer netPeer)
    {
        try
        {
            if (netPeer.Token != packet.Token) return; //Needs a session token at least.
            var entity = _world.GetEntity(packet.Id);
            if (entity == null) return;
            entity.CaveFactor = packet.Value;
        }
        finally
        {
            PacketPool<McApiSetEntityCaveFactorRequestPacket>.Return(packet);
        }
    }

    private void HandleSetEntityMuffleFactorRequestPacket(McApiSetEntityMuffleFactorRequestPacket packet,
        McApiNetPeer netPeer)
    {
        try
        {
            if (netPeer.Token != packet.Token) return; //Needs a session token at least.
            var entity = _world.GetEntity(packet.Id);
            if (entity == null) return;
            entity.MuffleFactor = packet.Value;
        }
        finally
        {
            PacketPool<McApiSetEntityMuffleFactorRequestPacket>.Return(packet);
        }
    }

    //Resharper disable All
    private class Rawtext
    {
        public RawtextMessage[] rawtext { get; set; } = [];
    }

    private class RawtextMessage
    {
        public string text { get; set; } = string.Empty;
    }
    //Resharper enable All
}