using System.Collections.Concurrent;
using System.Numerics;
using Spectre.Console;
using VoiceCraft.Core.Locales;
using VoiceCraft.Core.World;
using VoiceCraft.Network;
using VoiceCraft.Network.Interfaces;
using VoiceCraft.Network.NetPeers;
using VoiceCraft.Network.Packets.McApiPackets;
using VoiceCraft.Network.Packets.McApiPackets.Event;
using VoiceCraft.Network.Packets.VcPackets;
using VoiceCraft.Network.Packets.VcPackets.Event;
using VoiceCraft.Network.Packets.VcPackets.Request;
using VoiceCraft.Network.Servers;
using VoiceCraft.Network.Systems;
using VoiceCraft.Network.World;

namespace VoiceCraft.Server.Systems;

public class EventHandlerSystem : IDisposable
{
    private readonly AudioEffectSystem _audioEffectSystem;
    private readonly LiteNetVoiceCraftServer _liteNetServer;
    private readonly WebRtcVoiceCraftServer _webRtcServer;
    private readonly VoiceCraftServer[] _voiceCraftServers;
    private readonly IEnumerable<McApiServer> _mcApiServers;
    private readonly ConcurrentQueue<Action> _tasks = [];
    private readonly VoiceCraftWorld _world;
    public bool EnableVisibilityDisplay { get; set; }

    public EventHandlerSystem(
        LiteNetVoiceCraftServer liteNetServer,
        WebRtcVoiceCraftServer webRtcServer,
        IEnumerable<McApiServer> mcApiServers,
        AudioEffectSystem audioEffectSystem,
        VoiceCraftWorld world)
    {
        _liteNetServer = liteNetServer;
        _webRtcServer = webRtcServer;
        _voiceCraftServers = [liteNetServer, webRtcServer];
        _mcApiServers = mcApiServers; //Zero alloc.
        _audioEffectSystem = audioEffectSystem;
        _world = world;

        _world.OnEntityCreated += OnEntityCreated;
        _world.OnEntityDestroyed += OnEntityDestroyed;
        _audioEffectSystem.OnEffectSet += OnAudioEffectSet;
        foreach (var mcApiServer in _mcApiServers)
        {
            mcApiServer.OnPeerConnected += OnMcApiPeerConnected;
            mcApiServer.OnPeerDisconnected += OnMcApiPeerDisconnected;
        }
    }

    public void Dispose()
    {
        _world.OnEntityCreated -= OnEntityCreated;
        _world.OnEntityDestroyed -= OnEntityDestroyed;
        _audioEffectSystem.OnEffectSet -= OnAudioEffectSet;
        foreach (var mcApiServer in _mcApiServers)
        {
            mcApiServer.OnPeerConnected -= OnMcApiPeerConnected;
            mcApiServer.OnPeerDisconnected -= OnMcApiPeerDisconnected;
        }

        GC.SuppressFinalize(this);
    }

    public void Update()
    {
        while (_tasks.TryDequeue(out var result))
            try
            {
                result.Invoke();
            }
            catch (Exception ex)
            {
                LogService.Log(ex);
            }
    }

    private void BroadcastMcApi<T>(T packet) where T : class, IMcApiPacket
    {
        foreach (var mcApiServer in _mcApiServers)
        {
            mcApiServer.Broadcast(packet);
        }
    }

    private static void SendMcApi<T>(McApiServer server, McApiNetPeer peer, T packet) where T : class, IMcApiPacket
    {
        server.SendPacket(peer, packet);
    }

    private void SendVoiceCraft<T>(
        VoiceCraftNetPeer peer,
        T packet,
        VcDeliveryMethod deliveryMethod = VcDeliveryMethod.Reliable)
        where T : class, IVoiceCraftPacket
    {
        switch (peer)
        {
            case LiteNetVoiceCraftNetPeer:
                _liteNetServer.SendPacket(peer, packet, deliveryMethod);
                break;
            case WebRtcVoiceCraftNetPeer:
                _webRtcServer.SendPacket(peer, packet, deliveryMethod);
                break;
            default:
                PacketPool<T>.Return(packet);
                break;
        }
    }

    private void BroadcastVoiceCraft<T>(
        Func<T> packetFactory,
        VcDeliveryMethod deliveryMethod = VcDeliveryMethod.Reliable,
        params VoiceCraftNetPeer?[] excludes)
        where T : class, IVoiceCraftPacket
    {
        foreach (var voiceCraftServer in _voiceCraftServers)
            voiceCraftServer.Broadcast(packetFactory.Invoke(), deliveryMethod, excludes);
    }

    private void DisconnectVoiceCraft(VoiceCraftNetPeer peer, string reason)
    {
        switch (peer)
        {
            case LiteNetVoiceCraftNetPeer:
                _liteNetServer.Disconnect(peer, reason);
                break;
            case WebRtcVoiceCraftNetPeer:
                _webRtcServer.Disconnect(peer, reason);
                break;
        }
    }

    #region Audio Effect Events

    private void OnAudioEffectSet(ushort bitmask, IAudioEffect? effect)
    {
        _tasks.Enqueue(() =>
        {
            BroadcastVoiceCraft(() => PacketPool<VcOnEffectUpdatedPacket>
                .GetPacket(() => new VcOnEffectUpdatedPacket()).Set(bitmask, effect));
            BroadcastMcApi(PacketPool<McApiOnEffectUpdatedPacket>.GetPacket(() =>
                new McApiOnEffectUpdatedPacket()).Set(bitmask, effect));
        });
    }

    #endregion

    #region Entity Events

    //World
    private void OnEntityCreated(VoiceCraftEntity newEntity)
    {
        if (newEntity is VoiceCraftNetworkEntity netEntity)
        {
            netEntity.OnSetTitle += OnNetworkEntitySetTitle;
            netEntity.OnSetDescription += OnNetworkEntitySetDescription;
            netEntity.OnServerMuteUpdated += OnNetworkEntityServerMuteUpdated;
            netEntity.OnServerDeafenUpdated += OnNetworkEntityServerDeafenUpdated;
        }

        newEntity.OnWorldIdUpdated += OnEntityWorldIdUpdated;
        newEntity.OnNameUpdated += OnEntityNameUpdated;
        newEntity.OnMuteUpdated += OnEntityMuteUpdated;
        newEntity.OnDeafenUpdated += OnEntityDeafenUpdated;
        newEntity.OnTalkBitmaskUpdated += OnEntityTalkBitmaskUpdated;
        newEntity.OnListenBitmaskUpdated += OnEntityListenBitmaskUpdated;
        newEntity.OnEffectBitmaskUpdated += OnEntityEffectBitmaskUpdated;
        newEntity.OnPositionUpdated += OnEntityPositionUpdated;
        newEntity.OnRotationUpdated += OnEntityRotationUpdated;
        newEntity.OnCaveFactorUpdated += OnEntityCaveFactorUpdated;
        newEntity.OnMuffleFactorUpdated += OnEntityMuffleFactorUpdated;
        newEntity.OnVisibleEntityAdded += OnEntityVisibleEntityAdded;
        newEntity.OnVisibleEntityRemoved += OnEntityVisibleEntityRemoved;
        newEntity.OnAudioReceived += OnEntityAudioReceived;

        _tasks.Enqueue(() =>
        {
            if (newEntity is VoiceCraftNetworkEntity networkEntity)
            {
                SendVoiceCraft(networkEntity.NetPeer,
                    PacketPool<VcSetNameRequestPacket>.GetPacket(() => new VcSetNameRequestPacket())
                        .Set(networkEntity.Name));
                SendVoiceCraft(networkEntity.NetPeer,
                    PacketPool<VcSetServerMuteRequestPacket>.GetPacket(() => new VcSetServerMuteRequestPacket())
                        .Set(networkEntity.ServerMuted));
                SendVoiceCraft(networkEntity.NetPeer,
                    PacketPool<VcSetServerDeafenRequestPacket>.GetPacket(() => new VcSetServerDeafenRequestPacket())
                        .Set(networkEntity.ServerDeafened));
                BroadcastVoiceCraft(() => PacketPool<VcOnNetworkEntityCreatedPacket>
                    .GetPacket(() => new VcOnNetworkEntityCreatedPacket()).Set(networkEntity),
                    VcDeliveryMethod.Reliable,
                    networkEntity.NetPeer);
                if (!EnableVisibilityDisplay)
                {
                    BroadcastVoiceCraft(() => PacketPool<VcSetEntityVisibilityRequestPacket>
                            .GetPacket(() => new VcSetEntityVisibilityRequestPacket()).Set(networkEntity.Id, true),
                        VcDeliveryMethod.Reliable,
                        networkEntity.NetPeer);
                }

                BroadcastMcApi(PacketPool<McApiOnNetworkEntityCreatedPacket>
                    .GetPacket(() => new McApiOnNetworkEntityCreatedPacket()).Set(networkEntity));

                //Send Effects
                var audioEffects = _audioEffectSystem.AudioEffects;
                foreach (var effect in audioEffects)
                    SendVoiceCraft(networkEntity.NetPeer,
                        PacketPool<VcOnEffectUpdatedPacket>.GetPacket(() => new VcOnEffectUpdatedPacket())
                            .Set(effect.Key, effect.Value));

                //Send other entities.
                foreach (var entity in _world.Entities.Where(x => x != networkEntity))
                {
                    if (entity is VoiceCraftNetworkEntity otherNetworkEntity)
                        SendVoiceCraft(networkEntity.NetPeer,
                            PacketPool<VcOnNetworkEntityCreatedPacket>
                                .GetPacket(() => new VcOnNetworkEntityCreatedPacket()).Set(otherNetworkEntity));
                    else
                        SendVoiceCraft(networkEntity.NetPeer,
                            PacketPool<VcOnEntityCreatedPacket>.GetPacket(() => new VcOnEntityCreatedPacket())
                                .Set(entity));

                    if (EnableVisibilityDisplay) continue;
                    SendVoiceCraft(networkEntity.NetPeer, PacketPool<VcSetEntityVisibilityRequestPacket>
                        .GetPacket(() => new VcSetEntityVisibilityRequestPacket())
                        .Set(entity.Id, true));
                }

                AnsiConsole.MarkupLine(
                    $"[green]{Localizer.Get($"Events.Client.Connected:{networkEntity.UserGuid}")}[/]");
            }
            else
            {
                BroadcastVoiceCraft(() => PacketPool<VcOnEntityCreatedPacket>
                    .GetPacket(() => new VcOnEntityCreatedPacket()).Set(newEntity));
                BroadcastMcApi(PacketPool<McApiOnEntityCreatedPacket>.GetPacket(() => new McApiOnEntityCreatedPacket())
                    .Set(newEntity));
            }
        });
    }

    private void OnEntityDestroyed(VoiceCraftEntity entity)
    {
        if (entity is VoiceCraftNetworkEntity netEntity)
        {
            netEntity.OnSetTitle -= OnNetworkEntitySetTitle;
            netEntity.OnSetDescription -= OnNetworkEntitySetDescription;
            netEntity.OnServerMuteUpdated -= OnNetworkEntityServerMuteUpdated;
            netEntity.OnServerDeafenUpdated -= OnNetworkEntityServerDeafenUpdated;
        }

        entity.OnWorldIdUpdated -= OnEntityWorldIdUpdated;
        entity.OnNameUpdated -= OnEntityNameUpdated;
        entity.OnMuteUpdated -= OnEntityMuteUpdated;
        entity.OnDeafenUpdated -= OnEntityDeafenUpdated;
        entity.OnTalkBitmaskUpdated -= OnEntityTalkBitmaskUpdated;
        entity.OnListenBitmaskUpdated -= OnEntityListenBitmaskUpdated;
        entity.OnEffectBitmaskUpdated -= OnEntityEffectBitmaskUpdated;
        entity.OnPositionUpdated -= OnEntityPositionUpdated;
        entity.OnRotationUpdated -= OnEntityRotationUpdated;
        entity.OnCaveFactorUpdated -= OnEntityCaveFactorUpdated;
        entity.OnMuffleFactorUpdated -= OnEntityMuffleFactorUpdated;
        entity.OnVisibleEntityAdded -= OnEntityVisibleEntityAdded;
        entity.OnVisibleEntityRemoved -= OnEntityVisibleEntityRemoved;
        entity.OnAudioReceived -= OnEntityAudioReceived;

        _tasks.Enqueue(() =>
        {
            if (entity is VoiceCraftNetworkEntity networkEntity)
            {
                DisconnectVoiceCraft(networkEntity.NetPeer, "VoiceCraft.DisconnectReason.Kicked");
                AnsiConsole.MarkupLine(
                    $"[yellow]{Localizer.Get($"Events.Client.Disconnected:{networkEntity.UserGuid}")}[/]");
            }

            BroadcastVoiceCraft(() => PacketPool<VcOnEntityDestroyedPacket>
                .GetPacket(() => new VcOnEntityDestroyedPacket()).Set(entity.Id));
            BroadcastMcApi(PacketPool<McApiOnEntityDestroyedPacket>.GetPacket(() => new McApiOnEntityDestroyedPacket())
                .Set(entity.Id));
        });
    }

    private void OnMcApiPeerConnected(McApiNetPeer peer, string token)
    {
        _tasks.Enqueue(() =>
        {
            if (peer.Tag is not McApiServer server) return;

            //Send Effects
            var audioEffects = _audioEffectSystem.AudioEffects;
            foreach (var effect in audioEffects)
                SendMcApi(server, peer, PacketPool<McApiOnEffectUpdatedPacket>.GetPacket(() =>
                    new McApiOnEffectUpdatedPacket()).Set(effect.Key, effect.Value));

            //Send other entities.
            foreach (var entity in _world.Entities)
                if (entity is VoiceCraftNetworkEntity otherNetworkEntity)
                    SendMcApi(server, peer, PacketPool<McApiOnNetworkEntityCreatedPacket>
                        .GetPacket(() => new McApiOnNetworkEntityCreatedPacket()).Set(otherNetworkEntity));
                else
                    SendMcApi(server, peer, PacketPool<McApiOnEntityCreatedPacket>.GetPacket(() =>
                        new McApiOnEntityCreatedPacket()).Set(entity));

            AnsiConsole.MarkupLine($"[green]{Localizer.Get($"Events.McApi.Client.Connected:{token}")}[/]");
        });
    }

    private void OnMcApiPeerDisconnected(McApiNetPeer peer, string token)
    {
        _tasks.Enqueue(() =>
        {
            AnsiConsole.MarkupLine($"[yellow]{Localizer.Get($"Events.McApi.Client.Disconnected:{token}")}[/]");
        });
    }

    //Data
    private void OnNetworkEntitySetTitle(string title, VoiceCraftNetworkEntity entity)
    {
        _tasks.Enqueue(() =>
        {
            SendVoiceCraft(entity.NetPeer,
                PacketPool<VcSetTitleRequestPacket>.GetPacket(() => new VcSetTitleRequestPacket()).Set(title));
        });
    }

    private void OnNetworkEntitySetDescription(string description, VoiceCraftNetworkEntity entity)
    {
        _tasks.Enqueue(() =>
        {
            SendVoiceCraft(entity.NetPeer,
                PacketPool<VcSetDescriptionRequestPacket>.GetPacket(() => new VcSetDescriptionRequestPacket())
                    .Set(description));
        });
    }

    private void OnNetworkEntityServerMuteUpdated(bool muted, VoiceCraftNetworkEntity entity)
    {
        _tasks.Enqueue(() =>
        {
            SendVoiceCraft(entity.NetPeer,
                PacketPool<VcSetServerMuteRequestPacket>.GetPacket(() => new VcSetServerMuteRequestPacket())
                    .Set(muted));
            BroadcastVoiceCraft(() => PacketPool<VcOnEntityServerMuteUpdatedPacket>
                    .GetPacket(() => new VcOnEntityServerMuteUpdatedPacket()).Set(entity.Id, muted),
                VcDeliveryMethod.Reliable,
                entity.NetPeer);
            BroadcastMcApi(PacketPool<McApiOnEntityServerMuteUpdatedPacket>.GetPacket(() =>
                new McApiOnEntityServerMuteUpdatedPacket()).Set(entity.Id, muted));
        });
    }

    private void OnNetworkEntityServerDeafenUpdated(bool deafened, VoiceCraftNetworkEntity entity)
    {
        _tasks.Enqueue(() =>
        {
            SendVoiceCraft(entity.NetPeer,
                PacketPool<VcSetServerDeafenRequestPacket>.GetPacket(() => new VcSetServerDeafenRequestPacket())
                    .Set(deafened));
            BroadcastVoiceCraft(() => PacketPool<VcOnEntityServerDeafenUpdatedPacket>
                    .GetPacket(() => new VcOnEntityServerDeafenUpdatedPacket()).Set(entity.Id, deafened),
                VcDeliveryMethod.Reliable,
                entity.NetPeer);
            BroadcastMcApi(PacketPool<McApiOnEntityServerDeafenUpdatedPacket>.GetPacket(() =>
                new McApiOnEntityServerDeafenUpdatedPacket()).Set(entity.Id, deafened));
        });
    }

    private void OnEntityWorldIdUpdated(string worldId, VoiceCraftEntity entity)
    {
        _tasks.Enqueue(() =>
        {
            BroadcastMcApi(PacketPool<McApiOnEntityWorldIdUpdatedPacket>.GetPacket(() =>
                new McApiOnEntityWorldIdUpdatedPacket()).Set(entity.Id, worldId));
        });
    }

    private void OnEntityNameUpdated(string name, VoiceCraftEntity entity)
    {
        _tasks.Enqueue(() =>
        {
            if (entity is VoiceCraftNetworkEntity networkEntity)
            {
                if (networkEntity.PositioningType == PositioningType.Server)
                    SendVoiceCraft(networkEntity.NetPeer,
                        PacketPool<VcSetNameRequestPacket>.GetPacket(() => new VcSetNameRequestPacket()).Set(name));

                BroadcastVoiceCraft(() => PacketPool<VcOnEntityNameUpdatedPacket>
                        .GetPacket(() => new VcOnEntityNameUpdatedPacket()).Set(entity.Id, name),
                    VcDeliveryMethod.Reliable,
                    networkEntity.NetPeer);
            }
            else
            {
                BroadcastVoiceCraft(() => PacketPool<VcOnEntityNameUpdatedPacket>
                    .GetPacket(() => new VcOnEntityNameUpdatedPacket()).Set(entity.Id, name));
            }

            BroadcastMcApi(PacketPool<McApiOnEntityNameUpdatedPacket>
                .GetPacket(() => new McApiOnEntityNameUpdatedPacket()).Set(entity.Id, name));
        });
    }

    private void OnEntityMuteUpdated(bool mute, VoiceCraftEntity entity)
    {
        _tasks.Enqueue(() =>
        {
            if (entity is VoiceCraftNetworkEntity networkEntity)
                BroadcastVoiceCraft(() => PacketPool<VcOnEntityMuteUpdatedPacket>
                        .GetPacket(() => new VcOnEntityMuteUpdatedPacket()).Set(entity.Id, mute),
                    VcDeliveryMethod.Reliable,
                    networkEntity.NetPeer);
            else
                BroadcastVoiceCraft(() => PacketPool<VcOnEntityMuteUpdatedPacket>
                    .GetPacket(() => new VcOnEntityMuteUpdatedPacket()).Set(entity.Id, mute));

            BroadcastMcApi(PacketPool<McApiOnEntityMuteUpdatedPacket>.GetPacket(() =>
                new McApiOnEntityMuteUpdatedPacket()).Set(entity.Id, mute));
        });
    }

    private void OnEntityDeafenUpdated(bool deafen, VoiceCraftEntity entity)
    {
        _tasks.Enqueue(() =>
        {
            if (entity is VoiceCraftNetworkEntity networkEntity)
                BroadcastVoiceCraft(() => PacketPool<VcOnEntityDeafenUpdatedPacket>
                        .GetPacket(() => new VcOnEntityDeafenUpdatedPacket()).Set(entity.Id, deafen),
                    VcDeliveryMethod.Reliable,
                    networkEntity.NetPeer);
            else
                BroadcastVoiceCraft(() => PacketPool<VcOnEntityDeafenUpdatedPacket>
                    .GetPacket(() => new VcOnEntityDeafenUpdatedPacket()).Set(entity.Id, deafen));

            BroadcastMcApi(PacketPool<McApiOnEntityDeafenUpdatedPacket>.GetPacket(() =>
                new McApiOnEntityDeafenUpdatedPacket()).Set(entity.Id, deafen));
        });
    }

    private void OnEntityTalkBitmaskUpdated(ushort bitmask, VoiceCraftEntity entity)
    {
        _tasks.Enqueue(() =>
        {
            if (entity is VoiceCraftNetworkEntity networkEntity)
            {
                SendVoiceCraft(networkEntity.NetPeer,
                    PacketPool<VcSetTalkBitmaskRequestPacket>.GetPacket(() => new VcSetTalkBitmaskRequestPacket())
                        .Set(bitmask));
                BroadcastVoiceCraft(() => PacketPool<VcOnEntityTalkBitmaskUpdatedPacket>
                        .GetPacket(() => new VcOnEntityTalkBitmaskUpdatedPacket()).Set(entity.Id, bitmask),
                    VcDeliveryMethod.Reliable,
                    networkEntity.NetPeer);
            }
            else
            {
                BroadcastVoiceCraft(() => PacketPool<VcOnEntityTalkBitmaskUpdatedPacket>
                    .GetPacket(() => new VcOnEntityTalkBitmaskUpdatedPacket()).Set(entity.Id, bitmask));
            }

            BroadcastMcApi(PacketPool<McApiOnEntityTalkBitmaskUpdatedPacket>.GetPacket(() =>
                new McApiOnEntityTalkBitmaskUpdatedPacket()).Set(entity.Id, bitmask));
        });
    }

    private void OnEntityListenBitmaskUpdated(ushort bitmask, VoiceCraftEntity entity)
    {
        _tasks.Enqueue(() =>
        {
            if (entity is VoiceCraftNetworkEntity networkEntity)
            {
                SendVoiceCraft(networkEntity.NetPeer,
                    PacketPool<VcSetListenBitmaskRequestPacket>.GetPacket(() => new VcSetListenBitmaskRequestPacket())
                        .Set(bitmask));
                BroadcastVoiceCraft(() => PacketPool<VcOnEntityListenBitmaskUpdatedPacket>
                        .GetPacket(() => new VcOnEntityListenBitmaskUpdatedPacket()).Set(entity.Id, bitmask),
                    VcDeliveryMethod.Reliable,
                    networkEntity.NetPeer);
            }
            else
            {
                BroadcastVoiceCraft(() => PacketPool<VcOnEntityListenBitmaskUpdatedPacket>
                    .GetPacket(() => new VcOnEntityListenBitmaskUpdatedPacket()).Set(entity.Id, bitmask));
            }

            BroadcastMcApi(
                PacketPool<McApiOnEntityListenBitmaskUpdatedPacket>.GetPacket(() =>
                    new McApiOnEntityListenBitmaskUpdatedPacket()).Set(entity.Id, bitmask));
        });
    }

    private void OnEntityEffectBitmaskUpdated(ushort bitmask, VoiceCraftEntity entity)
    {
        _tasks.Enqueue(() =>
        {
            if (entity is VoiceCraftNetworkEntity networkEntity)
            {
                SendVoiceCraft(networkEntity.NetPeer,
                    PacketPool<VcSetEffectBitmaskRequestPacket>.GetPacket(() => new VcSetEffectBitmaskRequestPacket())
                        .Set(bitmask));
                BroadcastVoiceCraft(() => PacketPool<VcOnEntityEffectBitmaskUpdatedPacket>
                        .GetPacket(() => new VcOnEntityEffectBitmaskUpdatedPacket()).Set(entity.Id, bitmask),
                    VcDeliveryMethod.Reliable,
                    networkEntity.NetPeer);
            }
            else
            {
                BroadcastVoiceCraft(() => PacketPool<VcOnEntityEffectBitmaskUpdatedPacket>
                    .GetPacket(() => new VcOnEntityEffectBitmaskUpdatedPacket()).Set(entity.Id, bitmask));
            }

            BroadcastMcApi(PacketPool<McApiOnEntityEffectBitmaskUpdatedPacket>.GetPacket(() =>
                new McApiOnEntityEffectBitmaskUpdatedPacket()).Set(entity.Id, bitmask));
        });
    }

    //Properties
    private void OnEntityPositionUpdated(Vector3 position, VoiceCraftEntity entity)
    {
        _tasks.Enqueue(() =>
        {
            if (entity is VoiceCraftNetworkEntity { PositioningType: PositioningType.Server } networkEntity)
                SendVoiceCraft(networkEntity.NetPeer,
                    PacketPool<VcSetPositionRequestPacket>.GetPacket(() => new VcSetPositionRequestPacket())
                        .Set(position));

            foreach (var ve in entity.VisibleEntities)
            {
                if (ve is not VoiceCraftNetworkEntity visibleEntity) continue;
                var packet = PacketPool<VcOnEntityPositionUpdatedPacket>
                    .GetPacket(() => new VcOnEntityPositionUpdatedPacket()).Set(entity.Id, position);
                SendVoiceCraft(visibleEntity.NetPeer, packet);
            }

            BroadcastMcApi(PacketPool<McApiOnEntityPositionUpdatedPacket>.GetPacket(() =>
                new McApiOnEntityPositionUpdatedPacket()).Set(entity.Id, position));
        });
    }

    private void OnEntityRotationUpdated(Vector2 rotation, VoiceCraftEntity entity)
    {
        _tasks.Enqueue(() =>
        {
            if (entity is VoiceCraftNetworkEntity { PositioningType: PositioningType.Server } networkEntity)
                SendVoiceCraft(networkEntity.NetPeer,
                    PacketPool<VcSetRotationRequestPacket>.GetPacket(() => new VcSetRotationRequestPacket())
                        .Set(rotation));

            foreach (var ve in entity.VisibleEntities)
            {
                if (ve is not VoiceCraftNetworkEntity visibleEntity) continue;
                var packet = PacketPool<VcOnEntityRotationUpdatedPacket>
                    .GetPacket(() => new VcOnEntityRotationUpdatedPacket()).Set(entity.Id, rotation);
                SendVoiceCraft(visibleEntity.NetPeer, packet);
            }

            BroadcastMcApi(PacketPool<McApiOnEntityRotationUpdatedPacket>.GetPacket(() =>
                new McApiOnEntityRotationUpdatedPacket()).Set(entity.Id, rotation));
        });
    }

    private void OnEntityCaveFactorUpdated(float caveFactor, VoiceCraftEntity entity)
    {
        _tasks.Enqueue(() =>
        {
            if (entity is VoiceCraftNetworkEntity { PositioningType: PositioningType.Server } networkEntity)
                SendVoiceCraft(networkEntity.NetPeer,
                    PacketPool<VcSetCaveFactorRequest>.GetPacket(() => new VcSetCaveFactorRequest()).Set(caveFactor));

            foreach (var ve in entity.VisibleEntities)
            {
                if (ve is not VoiceCraftNetworkEntity visibleEntity) continue;
                var packet = PacketPool<VcOnEntityCaveFactorUpdatedPacket>
                    .GetPacket(() => new VcOnEntityCaveFactorUpdatedPacket()).Set(entity.Id, caveFactor);
                SendVoiceCraft(visibleEntity.NetPeer, packet);
            }

            BroadcastMcApi(PacketPool<McApiOnEntityCaveFactorUpdatedPacket>.GetPacket(() =>
                new McApiOnEntityCaveFactorUpdatedPacket()).Set(entity.Id, caveFactor));
        });
    }

    private void OnEntityMuffleFactorUpdated(float muffleFactor, VoiceCraftEntity entity)
    {
        _tasks.Enqueue(() =>
        {
            if (entity is VoiceCraftNetworkEntity { PositioningType: PositioningType.Server } networkEntity)
                SendVoiceCraft(networkEntity.NetPeer,
                    PacketPool<VcSetMuffleFactorRequest>.GetPacket(() => new VcSetMuffleFactorRequest())
                        .Set(muffleFactor));

            foreach (var ve in entity.VisibleEntities)
            {
                if (ve is not VoiceCraftNetworkEntity visibleEntity) continue;
                var packet = PacketPool<VcOnEntityMuffleFactorUpdatedPacket>
                    .GetPacket(() => new VcOnEntityMuffleFactorUpdatedPacket()).Set(entity.Id, muffleFactor);
                SendVoiceCraft(visibleEntity.NetPeer, packet);
            }

            BroadcastMcApi(PacketPool<McApiOnEntityMuffleFactorUpdatedPacket>.GetPacket(() =>
                new McApiOnEntityMuffleFactorUpdatedPacket()).Set(entity.Id, muffleFactor));
        });
    }

    //Visible Entities
    private void OnEntityVisibleEntityAdded(VoiceCraftEntity addedEntity, VoiceCraftEntity entity)
    {
        _tasks.Enqueue(() =>
        {
            if (addedEntity is VoiceCraftNetworkEntity networkEntity)
            {
                if (EnableVisibilityDisplay)
                {
                    var visibilityPacket = PacketPool<VcSetEntityVisibilityRequestPacket>
                        .GetPacket(() => new VcSetEntityVisibilityRequestPacket())
                        .Set(entity.Id, true);
                    SendVoiceCraft(networkEntity.NetPeer, visibilityPacket);
                }

                var positionPacket = PacketPool<VcOnEntityPositionUpdatedPacket>
                    .GetPacket(() => new VcOnEntityPositionUpdatedPacket())
                    .Set(entity.Id, entity.Position);
                var rotationPacket = PacketPool<VcOnEntityRotationUpdatedPacket>
                    .GetPacket(() => new VcOnEntityRotationUpdatedPacket())
                    .Set(entity.Id, entity.Rotation);
                var caveFactorPacket = PacketPool<VcOnEntityCaveFactorUpdatedPacket>
                    .GetPacket(() => new VcOnEntityCaveFactorUpdatedPacket())
                    .Set(entity.Id, entity.CaveFactor);
                var muffleFactorPacket = PacketPool<VcOnEntityMuffleFactorUpdatedPacket>
                    .GetPacket(() => new VcOnEntityMuffleFactorUpdatedPacket())
                    .Set(entity.Id, entity.MuffleFactor);

                SendVoiceCraft(networkEntity.NetPeer, positionPacket);
                SendVoiceCraft(networkEntity.NetPeer, rotationPacket);
                SendVoiceCraft(networkEntity.NetPeer, caveFactorPacket);
                SendVoiceCraft(networkEntity.NetPeer, muffleFactorPacket);
            }

            BroadcastMcApi(PacketPool<McApiOnEntityVisibilityUpdatedPacket>.GetPacket(() =>
                new McApiOnEntityVisibilityUpdatedPacket()).Set(entity.Id, addedEntity.Id, true));
        });
    }

    private void OnEntityVisibleEntityRemoved(VoiceCraftEntity removedEntity, VoiceCraftEntity entity)
    {
        _tasks.Enqueue(() =>
        {
            if (removedEntity is VoiceCraftNetworkEntity networkEntity)
            {
                if (EnableVisibilityDisplay)
                {
                    SendVoiceCraft(networkEntity.NetPeer,
                        PacketPool<VcSetEntityVisibilityRequestPacket>
                            .GetPacket(() => new VcSetEntityVisibilityRequestPacket()).Set(entity.Id));
                }
            }

            BroadcastMcApi(PacketPool<McApiOnEntityVisibilityUpdatedPacket>.GetPacket(() =>
                new McApiOnEntityVisibilityUpdatedPacket()).Set(entity.Id, removedEntity.Id));
        });
    }

    private void OnEntityAudioReceived(byte[] buffer, ushort timestamp, float frameLoudness, VoiceCraftEntity entity)
    {
        _tasks.Enqueue(() =>
        {
            foreach (var ve in entity.VisibleEntities)
            {
                if (ve is not VoiceCraftNetworkEntity visibleEntity || ve == entity || visibleEntity.Deafened ||
                    visibleEntity.ServerDeafened) continue;
                var packet = PacketPool<VcOnEntityAudioReceivedPacket>
                    .GetPacket(() => new VcOnEntityAudioReceivedPacket())
                    .Set(entity.Id, timestamp, frameLoudness, buffer.Length, buffer);
                SendVoiceCraft(visibleEntity.NetPeer, packet, VcDeliveryMethod.Unreliable);
            }

            BroadcastMcApi(PacketPool<McApiOnEntityAudioReceivedPacket>.GetPacket(() =>
                new McApiOnEntityAudioReceivedPacket()).Set(
                entity.Id,
                timestamp,
                frameLoudness,
                buffer.Length,
                buffer));
        });
    }

    #endregion
}
