using System.Collections.Concurrent;
using System.Text.Json;
using LiteNetLib.Utils;
using Spectre.Console;
using VoiceCraft.Core;
using VoiceCraft.Core.Audio.Effects;
using VoiceCraft.Core.Network;
using VoiceCraft.Core.Network.McApiPackets;
using VoiceCraft.Core.Network.McApiPackets.Request;
using VoiceCraft.Core.Network.McApiPackets.Response;
using VoiceCraft.Core.Network.McHttpPackets;
using VoiceCraft.Core.World;
using VoiceCraft.Server.Config;
using VoiceCraft.Server.Systems;
using WatsonWebserver.Core;
using WatsonWebserver.Lite;

namespace VoiceCraft.Server.Servers;

public class McHttpServer(VoiceCraftWorld world, AudioEffectSystem audioEffectSystem)
{
    private static readonly Version McHttpVersion = new(Constants.Minor, Constants.Major, 0);

    private readonly ConcurrentDictionary<string, McApiNetPeer> _mcApiPeers = [];
    private readonly NetDataReader _reader = new();
    private readonly NetDataWriter _writer = new();
    private readonly VoiceCraftWorld _world = world;
    private readonly AudioEffectSystem _audioEffectSystem = audioEffectSystem;
    private WebserverLite? _httpServer;

    //Config
    public McHttpConfig Config { get; private set; } = new(); 
    
    //Events
    public event Action<McApiNetPeer>? OnPeerConnected;
    public event Action<McApiNetPeer>? OnPeerDisconnected;

    public void Start(McHttpConfig? config = null)
    {
        if (config != null)
            Config = config;

        try
        {
            AnsiConsole.WriteLine(Locales.Locales.McHttpServer_Starting);
            var settings = new WebserverSettings();
            _httpServer = new WebserverLite(settings, HandleRequest);
            _httpServer.Start();
            AnsiConsole.MarkupLine($"[green]{Locales.Locales.McHttpServer_Success}[/]");
        }
        catch
        {
            throw new Exception(Locales.Locales.McHttpServer_Exceptions_Failed);
        }
    }

    public void Update()
    {
        if (_httpServer == null) return;
        foreach (var peer in _mcApiPeers) UpdatePeer(peer);
    }

    public void Stop()
    {
        if (_httpServer == null) return;
        AnsiConsole.WriteLine(Locales.Locales.McHttpServer_Stopping);
        _httpServer.Stop();
        _httpServer.Dispose();
        _httpServer = null;
        AnsiConsole.MarkupLine($"[green]{Locales.Locales.McHttpServer_Stopped}[/]");
    }

    public void SendPacket<T>(McApiNetPeer netPeer, T packet) where T : IMcApiPacket
    {
        if (_httpServer == null) return;
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
        if (_httpServer == null) return;
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

    private async Task HandleRequest(HttpContextBase context)
    {
        try
        {
            if (context.Request.ContentLength >= 1e+6) //Do not accept anything higher than a mb.
            {
                context.Response.StatusCode = 413;
                await context.Response.Send();
                return;
            }
            
            var packet = await JsonSerializer.DeserializeAsync<McHttpUpdatePacket>(context.Request.Data);
            if (packet == null)
            {
                context.Response.StatusCode = 400;
                await context.Response.Send();
                return;
            }
            
            var netPeer = GetOrCreatePeer(context.Request.Source.IpAddress);
            foreach(var data in packet.Packets)
            {
                if (data.Length <= 0) continue;
                netPeer.ReceiveInboundPacket(Z85.GetBytesWithPadding(data));
            }
            
            var sendData = new List<string>();
            while (netPeer.RetrieveOutboundPacket(out var outboundPacket))
            {
                sendData.Add(Z85.GetStringWithPadding(outboundPacket));
            }
            packet.Packets = sendData.ToArray();
            var responseData = JsonSerializer.Serialize(packet);
            context.Response.StatusCode = 200;
            await context.Response.Send(responseData);
        }
        catch (JsonException)
        {
            context.Response.StatusCode = 400;
            await context.Response.Send();
        }
        catch
        {
            context.Response.StatusCode = 500;
            await context.Response.Send();
        }
    }

    private McApiNetPeer GetOrCreatePeer(string ipAddress)
    {
        return _mcApiPeers.GetOrAdd(ipAddress, _ =>
        { 
            var netPeer = new McApiNetPeer();
            netPeer.OnConnected += McApiNetPeerOnConnected;
            netPeer.OnDisconnected += McApiNetPeerOnDisconnected;
            return netPeer;
        });
    }

    private void RemovePeer(string ipAddress)
    {
        _mcApiPeers.TryRemove(ipAddress, out _);
    }
    
    private void UpdatePeer(KeyValuePair<string, McApiNetPeer> peer)
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
                    HandlePacket(pt, _reader, peer.Value);
                }
                catch
                {
                    //Do Nothing
                }
        }

        if (DateTime.UtcNow - peer.Value.LastPing < TimeSpan.FromMilliseconds(Config.MaxTimeoutMs)) return;
        peer.Value.Disconnect();
        RemovePeer(peer.Key);
    }
    
    private void HandlePacket(McApiPacketType packetType, NetDataReader reader, McApiNetPeer peer)
    {
        if (packetType == McApiPacketType.LoginRequest)
        {
            var loginRequestPacket = PacketPool<McApiLoginRequestPacket>.GetPacket();
            loginRequestPacket.Deserialize(reader);
            HandleLoginRequestPacket(loginRequestPacket, peer);
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

    private void HandleLoginRequestPacket(McApiLoginRequestPacket packet, McApiNetPeer netPeer)
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
                SendPacket(netPeer, PacketPool<McApiDenyResponsePacket>.GetPacket().Set(packet.RequestId, packet.Token,
                    "VcMcApi.DisconnectReason.InvalidLoginToken"));
                return;
            }

            if (packet.Version.Major != McHttpVersion.Major || packet.Version.Minor != McHttpVersion.Minor)
            {
                SendPacket(netPeer, PacketPool<McApiDenyResponsePacket>.GetPacket().Set(packet.RequestId, packet.Token,
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

    private void HandleSetEffectRequestPacket(McApiSetEffectRequestPacket packet, McApiNetPeer netPeer,
        NetDataReader reader)
    {
        try
        {
            if (netPeer.Token != packet.Token) return; //Needs a session token at least
            if (_audioEffectSystem.TryGetEffect(packet.Bitmask, out var effect) &&
                effect.EffectType == packet.EffectType)
            {
                return;
            }

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

    private void HandleClearEffectsRequestPacket(McApiClearEffectsRequestPacket packet, McApiNetPeer netPeer)
    {
        try
        {
            if (netPeer.Token != packet.Token) return; //Needs a session token at least.
            _audioEffectSystem.ClearEffects();
        }
        finally
        {
            PacketPool<McApiClearEffectsRequestPacket>.Return(packet);
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
}