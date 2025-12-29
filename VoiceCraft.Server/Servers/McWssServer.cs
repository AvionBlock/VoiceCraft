using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using LiteNetLib.Utils;
using Spectre.Console;
using VoiceCraft.Core;
using VoiceCraft.Core.Network;
using VoiceCraft.Core.Network.McApiPackets;
using VoiceCraft.Core.Network.McWssPackets;
using VoiceCraft.Server.Config;
using Fleck;
using VoiceCraft.Core.Audio.Effects;
using VoiceCraft.Core.Locales;
using VoiceCraft.Core.Network.McApiPackets.Request;
using VoiceCraft.Core.Network.McApiPackets.Response;
using VoiceCraft.Core.World;
using VoiceCraft.Server.Systems;

namespace VoiceCraft.Server.Servers;

public class McWssServer(VoiceCraftWorld world, AudioEffectSystem audioEffectSystem)
{
    private static readonly Version McWssVersion = new(Constants.Major, Constants.Minor, 0);

    private readonly ConcurrentDictionary<IWebSocketConnection, McApiNetPeer> _mcApiPeers = [];
    private readonly NetDataReader _reader = new();
    private readonly NetDataWriter _writer = new();
    private readonly VoiceCraftWorld _world = world;
    private readonly AudioEffectSystem _audioEffectSystem = audioEffectSystem;
    private WebSocketServer? _wsServer;

    //Public Properties
    public McWssConfig Config { get; private set; } = new();

    //Events
    public event Action<McApiNetPeer, string>? OnPeerConnected;
    public event Action<McApiNetPeer, string>? OnPeerDisconnected;

    public void Start(McWssConfig? config = null)
    {
        Stop();

        if (config != null)
            Config = config;

        //Will turn into errors later.
        Config.CommandsPerTick = Math.Clamp(Config.CommandsPerTick, 1, 100);
        Config.MaxStringLengthPerCommand = Math.Clamp(Config.MaxStringLengthPerCommand, 1, 10000);
        try
        {
            AnsiConsole.WriteLine(Localizer.Get("McWssServer.Starting"));
            _wsServer = new WebSocketServer(Config.Hostname);

            _wsServer.Start(socket =>
            {
                socket.OnOpen = () => OnClientConnected(socket);
                socket.OnClose = () => OnClientDisconnected(socket);
                socket.OnMessage = message => OnMessageReceived(socket, message);
            });
            AnsiConsole.MarkupLine($"[green]{Localizer.Get("McWssServer.Success")}[/]");
        }
        catch(Exception ex)
        {
            AnsiConsole.WriteException(ex);
            LogService.Log(ex);
            throw new Exception(Localizer.Get("McWssServer.Exceptions.Failed"), ex);
        }
    }

    public void Update()
    {
        if (_wsServer == null) return;
        foreach (var peer in _mcApiPeers) UpdatePeer(peer);
    }

    public void Stop()
    {
        if (_wsServer == null) return;
        AnsiConsole.WriteLine(Localizer.Get("McWssServer.Stopping"));
        foreach (var client in _mcApiPeers)
        {
            try
            {
                client.Key.Close();
            }
            catch (Exception ex)
            {
                AnsiConsole.WriteException(ex);
                LogService.Log(ex);
            }
        }

        _wsServer.Dispose();
        _wsServer = null;
        AnsiConsole.MarkupLine($"[green]{Localizer.Get("McWssServer.Stopped")}[/]");
    }

    public void SendPacket<T>(McApiNetPeer netPeer, T packet) where T : IMcApiPacket
    {
        if (_wsServer == null) return;
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
        if (_wsServer == null) return;
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

    private void SendPacketCommand(IWebSocketConnection socket, string packetData)
    {
        var packet = new McWssCommandRequest($"{Config.DataTunnelCommand} \"{packetData}\"");
        socket.Send(JsonSerializer.Serialize(packet));
    }

    private void UpdatePeer(KeyValuePair<IWebSocketConnection, McApiNetPeer> peer)
    {
        lock (_reader)
        {
            while (peer.Value.RetrieveInboundPacket(out var packetData, out var token))
                try
                {
                    _reader.Clear();
                    _reader.SetSource(packetData);
                    var packetType = _reader.GetByte();
                    var pt = (McApiPacketType)packetType;
                    HandlePacket(pt, _reader, peer.Value, token);
                }
                catch(Exception ex)
                {
                    LogService.Log(ex);
                }
        }

        for (var i = 0; i < Config.CommandsPerTick; i++)
        {
            if (!SendPacketsLogic(peer.Key, peer.Value))
            {
                SendPacketCommand(peer.Key, string.Empty);
            }
        }

        switch (peer.Value.Connected)
        {
            case true when DateTime.UtcNow - peer.Value.LastPing >= TimeSpan.FromMilliseconds(Config.MaxTimeoutMs):
                peer.Value.Disconnect();
                break;
        }
    }

    private bool SendPacketsLogic(IWebSocketConnection socket, McApiNetPeer netPeer)
    {
        var stringBuilder = new StringBuilder();
        if (!netPeer.RetrieveOutboundPacket(out var outboundPacket)) return false;
        stringBuilder.Append(Z85.GetStringWithPadding(outboundPacket));
        while (netPeer.RetrieveOutboundPacket(out outboundPacket) && stringBuilder.Length < Config.MaxStringLengthPerCommand)
        {
            stringBuilder.Append($"|{Z85.GetStringWithPadding(outboundPacket)}");
        }
        SendPacketCommand(socket, stringBuilder.ToString());
        return true;
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
            if (genericPacket == null || genericPacket.header.messagePurpose != "commandResponse") return;

            var commandResponsePacket = JsonSerializer.Deserialize<McWssCommandResponse>(message);
            if (commandResponsePacket is not { StatusCode: 0 }) return;
            HandleDataTunnelCommandResponse(socket, commandResponsePacket.body.statusMessage);
        }
        catch(Exception ex)
        {
            LogService.Log(ex);
        }
    }

    private void HandleDataTunnelCommandResponse(IWebSocketConnection socket, string data)
    {
        try
        {
            if (!_mcApiPeers.TryGetValue(socket, out var peer) || string.IsNullOrWhiteSpace(data)) return;
            var packets = data.Split("|");

            foreach (var packet in packets)
            {
                peer.ReceiveInboundPacket(Z85.GetBytesWithPadding(packet), null);
            }
        }
        catch(Exception ex)
        {
            LogService.Log(ex);
        }
    }

    private void HandlePacket(McApiPacketType packetType, NetDataReader reader, McApiNetPeer peer, string? _)
    {
        switch (packetType)
        {
            case McApiPacketType.LoginRequest:
            {
                var loginRequestPacket = PacketPool<McApiLoginRequestPacket>.GetPacket();
                loginRequestPacket.Deserialize(reader);
                HandleLoginRequestPacket(loginRequestPacket, peer);
                return;
            }
            case McApiPacketType.LogoutRequest:
            {
                var logoutRequestPacket = PacketPool<McApiLogoutRequestPacket>.GetPacket();
                logoutRequestPacket.Deserialize(reader);
                HandleLogoutRequestPacket(logoutRequestPacket, peer);
                return;
            }
        }

        if (!peer.Connected) return;
        //Don't need to check the token as MCWss is session based.

        switch (packetType)
        {
            case McApiPacketType.PingRequest:
                var pingRequestPacket = PacketPool<McApiPingRequestPacket>.GetPacket();
                pingRequestPacket.Deserialize(reader);
                HandlePingRequestPacket(pingRequestPacket, peer);
                break;
            case McApiPacketType.ResetRequest:
                var resetRequestPacket = PacketPool<McApiResetRequestPacket>.GetPacket();
                resetRequestPacket.Deserialize(reader);
                HandleResetRequestPacket(resetRequestPacket, peer);
                break;
            case McApiPacketType.SetEffectRequest:
                var setEffectRequestPacket = PacketPool<McApiSetEffectRequestPacket>.GetPacket();
                setEffectRequestPacket.Deserialize(reader);
                HandleSetEffectRequestPacket(setEffectRequestPacket, peer, reader);
                break;
            case McApiPacketType.ClearEffectsRequest:
                var clearEffectsRequestPacket = PacketPool<McApiClearEffectsRequestPacket>.GetPacket();
                clearEffectsRequestPacket.Deserialize(reader);
                HandleClearEffectsRequestPacket(clearEffectsRequestPacket, peer);
                break;
            case McApiPacketType.CreateEntityRequest:
                var createEntityRequestPacket = PacketPool<McApiCreateEntityRequestPacket>.GetPacket();
                createEntityRequestPacket.Deserialize(reader);
                HandleCreateEntityRequestPacket(createEntityRequestPacket, peer);
                break;
            case McApiPacketType.DestroyEntityRequest:
                var destroyEntityRequestPacket = PacketPool<McApiDestroyEntityRequestPacket>.GetPacket();
                destroyEntityRequestPacket.Deserialize(reader);
                HandleDestroyEntityRequestPacket(destroyEntityRequestPacket, peer);
                break;
            case McApiPacketType.EntityAudioRequest:
                var entityAudioRequestPacket = PacketPool<McApiEntityAudioRequestPacket>.GetPacket();
                entityAudioRequestPacket.Deserialize(reader);
                HandleEntityAudioRequestPacket(entityAudioRequestPacket, peer);
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

    private void McApiNetPeerOnConnected(McApiNetPeer peer, string token)
    {
        OnPeerConnected?.Invoke(peer, token);
    }

    private void McApiNetPeerOnDisconnected(McApiNetPeer peer, string token)
    {
        OnPeerDisconnected?.Invoke(peer, token);
    }

    private void HandleLoginRequestPacket(McApiLoginRequestPacket packet, McApiNetPeer netPeer)
    {
        try
        {
            if (netPeer.Connected)
            {
                SendPacket(netPeer,
                    PacketPool<McApiAcceptResponsePacket>.GetPacket().Set(packet.RequestId, netPeer.Token));
                OnPeerConnected?.Invoke(netPeer, netPeer.Token);
                return;
            }

            if (!string.IsNullOrEmpty(Config.LoginToken) && Config.LoginToken != packet.Token)
            {
                SendPacket(netPeer,
                    PacketPool<McApiDenyResponsePacket>.GetPacket()
                        .Set(packet.RequestId, "VcMcApi.DisconnectReason.InvalidLoginToken"));
                return;
            }

            if (packet.Version.Major != McWssVersion.Major || packet.Version.Minor != McWssVersion.Minor)
            {
                SendPacket(netPeer,
                    PacketPool<McApiDenyResponsePacket>.GetPacket().Set(packet.RequestId,
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
            if (packet.Token != netPeer.Token) return;
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
            SendPacket(netPeer, PacketPool<McApiPingResponsePacket>.GetPacket().Set());
        }
        finally
        {
            PacketPool<McApiPingRequestPacket>.Return(packet);
        }
    }
    
    private void HandleResetRequestPacket(McApiResetRequestPacket packet, McApiNetPeer netPeer)
    {
        try
        {
            _world.Reset();
            _audioEffectSystem.Reset();
            SendPacket(netPeer, PacketPool<McApiResetResponsePacket>.GetPacket().Set(packet.RequestId));
        }
        catch(Exception ex)
        {
            SendPacket(netPeer, PacketPool<McApiResetResponsePacket>.GetPacket()
                .Set(packet.RequestId, McApiResetResponsePacket.ResponseCodes.Failure));
            LogService.Log(ex);
        }
        finally
        {
            PacketPool<McApiResetRequestPacket>.Return(packet);
        }
    }

    private void HandleSetEffectRequestPacket(McApiSetEffectRequestPacket packet, McApiNetPeer _, NetDataReader reader)
    {
        try
        {
            switch (packet.EffectType)
            {
                case EffectType.Visibility:
                    var visibilityEffect = new VisibilityEffect();
                    visibilityEffect.Deserialize(reader);
                    _audioEffectSystem.SetEffect(packet.Bitmask, visibilityEffect);
                    break;
                case EffectType.Proximity:
                    var proximityEffect = new ProximityEffect();
                    proximityEffect.Deserialize(reader);
                    _audioEffectSystem.SetEffect(packet.Bitmask, proximityEffect);
                    break;
                case EffectType.Directional:
                    var directionalEffect = new DirectionalEffect();
                    directionalEffect.Deserialize(reader);
                    _audioEffectSystem.SetEffect(packet.Bitmask, directionalEffect);
                    break;
                case EffectType.ProximityEcho:
                    var proximityEchoEffect = new ProximityEchoEffect();
                    proximityEchoEffect.Deserialize(reader);
                    _audioEffectSystem.SetEffect(packet.Bitmask, proximityEchoEffect);
                    break;
                case EffectType.Echo:
                    var echoEffect = new EchoEffect();
                    echoEffect.Deserialize(reader);
                    _audioEffectSystem.SetEffect(packet.Bitmask, echoEffect);
                    break;
                case EffectType.ProximityMuffle:
                    var proximityMuffleEffect = new ProximityMuffleEffect();
                    proximityMuffleEffect.Deserialize(reader);
                    _audioEffectSystem.SetEffect(packet.Bitmask, proximityMuffleEffect);
                    break;
                case EffectType.Muffle:
                    var muffleEffect = new MuffleEffect();
                    muffleEffect.Deserialize(reader);
                    _audioEffectSystem.SetEffect(packet.Bitmask, muffleEffect);
                    break;
                case EffectType.None:
                    _audioEffectSystem.SetEffect(packet.Bitmask, null);
                    break;
                default: //Unknown, We don't do anything.
                    return;
            }
        }
        finally
        {
            PacketPool<McApiSetEffectRequestPacket>.Return(packet);
        }
    }

    private void HandleClearEffectsRequestPacket(McApiClearEffectsRequestPacket packet, McApiNetPeer _)
    {
        try
        {
            _audioEffectSystem.ClearEffects();
        }
        finally
        {
            PacketPool<McApiClearEffectsRequestPacket>.Return(packet);
        }
    }
    
    private void HandleCreateEntityRequestPacket(McApiCreateEntityRequestPacket packet, McApiNetPeer netPeer)
    {
        try
        {
            var entity = _world.CreateEntity(
                packet.WorldId,
                packet.Name,
                packet.Muted,
                packet.Deafened,
                packet.TalkBitmask,
                packet.ListenBitmask,
                packet.EffectBitmask,
                packet.Position,
                packet.Rotation,
                packet.CaveFactor,
                packet.MuffleFactor);
            SendPacket(netPeer, PacketPool<McApiCreateEntityResponsePacket>.GetPacket()
                .Set(packet.RequestId, McApiCreateEntityResponsePacket.ResponseCodes.Ok, entity.Id));
        }
        catch
        {
            SendPacket(netPeer,
                PacketPool<McApiCreateEntityResponsePacket>.GetPacket().Set(packet.RequestId,
                    McApiCreateEntityResponsePacket.ResponseCodes.Failure));
            throw;
        }
        finally
        {
            PacketPool<McApiCreateEntityRequestPacket>.Return(packet);
        }
    }

    private void HandleDestroyEntityRequestPacket(McApiDestroyEntityRequestPacket packet, McApiNetPeer netPeer)
    {
        try
        {
            _world.DestroyEntity(packet.Id);
            SendPacket(netPeer, PacketPool<McApiDestroyEntityResponsePacket>.GetPacket()
                .Set(packet.RequestId));
        }
        catch
        {
            SendPacket(netPeer, PacketPool<McApiDestroyEntityResponsePacket>.GetPacket()
                .Set(packet.RequestId, McApiDestroyEntityResponsePacket.ResponseCodes.NotFound));
            throw;
        }
        finally
        {
            PacketPool<McApiDestroyEntityRequestPacket>.Return(packet);
        }
    }
    
    private void HandleEntityAudioRequestPacket(McApiEntityAudioRequestPacket packet, McApiNetPeer _)
    {
        try
        {
            var entity = _world.GetEntity(packet.Id);
            if (entity is null or VoiceCraftNetworkEntity) return;
            entity.ReceiveAudio(packet.Data, packet.Timestamp, packet.FrameLoudness);
        }
        finally
        {
            PacketPool<McApiEntityAudioRequestPacket>.Return(packet);
        }
    }

    private void HandleSetEntityTitleRequestPacket(McApiSetEntityTitleRequestPacket packet, McApiNetPeer _)
    {
        try
        {
            var entity = _world.GetEntity(packet.Id);
            if (entity is not VoiceCraftNetworkEntity networkEntity) return;
            networkEntity.SetTitle(packet.Value);
        }
        finally
        {
            PacketPool<McApiSetEntityTitleRequestPacket>.Return(packet);
        }
    }

    private void HandleSetEntityDescriptionRequestPacket(McApiSetEntityDescriptionRequestPacket packet, McApiNetPeer _)
    {
        try
        {
            var entity = _world.GetEntity(packet.Id);
            if (entity is not VoiceCraftNetworkEntity networkEntity) return;
            networkEntity.SetDescription(packet.Value);
        }
        finally
        {
            PacketPool<McApiSetEntityDescriptionRequestPacket>.Return(packet);
        }
    }

    private void HandleSetEntityWorldIdRequestPacket(McApiSetEntityWorldIdRequestPacket packet, McApiNetPeer _)
    {
        try
        {
            var entity = _world.GetEntity(packet.Id);
            if (entity is null or VoiceCraftNetworkEntity { PositioningType: PositioningType.Client }) return;
            entity.WorldId = packet.Value;
        }
        finally
        {
            PacketPool<McApiSetEntityWorldIdRequestPacket>.Return(packet);
        }
    }

    private void HandleSetEntityNameRequestPacket(McApiSetEntityNameRequestPacket packet, McApiNetPeer _)
    {
        try
        {
            var entity = _world.GetEntity(packet.Id);
            if (entity is null or VoiceCraftNetworkEntity { PositioningType: PositioningType.Client }) return;
            entity.Name = packet.Value;
        }
        finally
        {
            PacketPool<McApiSetEntityNameRequestPacket>.Return(packet);
        }
    }

    private void HandleSetEntityTalkBitmaskRequestPacket(McApiSetEntityTalkBitmaskRequestPacket packet, McApiNetPeer _)
    {
        try
        {
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
        McApiNetPeer _)
    {
        try
        {
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
        McApiNetPeer _)
    {
        try
        {
            var entity = _world.GetEntity(packet.Id);
            if (entity == null) return;
            entity.EffectBitmask = packet.Value;
        }
        finally
        {
            PacketPool<McApiSetEntityEffectBitmaskRequestPacket>.Return(packet);
        }
    }

    private void HandleSetEntityPositionRequestPacket(McApiSetEntityPositionRequestPacket packet, McApiNetPeer _)
    {
        try
        {
            var entity = _world.GetEntity(packet.Id);
            if (entity is null or VoiceCraftNetworkEntity { PositioningType: PositioningType.Client }) return;
            entity.Position = packet.Value;
        }
        finally
        {
            PacketPool<McApiSetEntityPositionRequestPacket>.Return(packet);
        }
    }

    private void HandleSetEntityRotationRequestPacket(McApiSetEntityRotationRequestPacket packet, McApiNetPeer _)
    {
        try
        {
            var entity = _world.GetEntity(packet.Id);
            if (entity is null or VoiceCraftNetworkEntity { PositioningType: PositioningType.Client }) return;
            entity.Rotation = packet.Value;
        }
        finally
        {
            PacketPool<McApiSetEntityRotationRequestPacket>.Return(packet);
        }
    }

    private void HandleSetEntityCaveFactorRequestPacket(McApiSetEntityCaveFactorRequestPacket packet, McApiNetPeer _)
    {
        try
        {
            var entity = _world.GetEntity(packet.Id);
            if (entity is null or VoiceCraftNetworkEntity { PositioningType: PositioningType.Client }) return;
            entity.CaveFactor = packet.Value;
        }
        finally
        {
            PacketPool<McApiSetEntityCaveFactorRequestPacket>.Return(packet);
        }
    }

    private void HandleSetEntityMuffleFactorRequestPacket(McApiSetEntityMuffleFactorRequestPacket packet,
        McApiNetPeer _)
    {
        try
        {
            var entity = _world.GetEntity(packet.Id);
            if (entity is null or VoiceCraftNetworkEntity { PositioningType: PositioningType.Client }) return;
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