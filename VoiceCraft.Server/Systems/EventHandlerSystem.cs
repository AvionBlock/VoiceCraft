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
using VoiceCraft.Network.Packets.McApiPackets.Request;
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
    private readonly IEnumerable<VoiceCraftServer> _voiceCraftServers;
    private readonly IEnumerable<McApiServer> _mcApiServers;
    private readonly ConcurrentQueue<Action> _tasks = [];
    private readonly VoiceCraftWorld _world;
    public bool EnableVisibilityDisplay { get; set; }

    public EventHandlerSystem(
        IEnumerable<VoiceCraftServer> voiceCraftServers,
        IEnumerable<McApiServer> mcApiServers,
        AudioEffectSystem audioEffectSystem,
        VoiceCraftWorld world)
    {
        _voiceCraftServers = voiceCraftServers;
        _mcApiServers = mcApiServers;
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

    private static void Disconnect(VoiceCraftNetPeer peer, string reason, bool force = false)
    {
        peer.Server?.Disconnect(peer, reason, force);
    }

    private static void DisconnectMcApi(McApiNetPeer peer, bool force = false)
    {
        peer.Server?.Disconnect(peer, force);
    }

    private static void Send<T>(VoiceCraftNetPeer peer, T packet,
        VcDeliveryMethod deliveryMethod = VcDeliveryMethod.Reliable) where T : class, IVoiceCraftPacket
    {
        peer.Server?.SendPacket(peer, packet, deliveryMethod);
    }

    private static void SendEvent<T>(VoiceCraftNetPeer peer, T packet,
        VcDeliveryMethod deliveryMethod = VcDeliveryMethod.Reliable) where T : class, IVoiceCraftEventPacket
    {
        peer.Server?.SendPacket(
            peer,
            PacketPool<VcEventRequestPacket>
                .GetPacket(() => new VcEventRequestPacket())
                .Set(packet),
            deliveryMethod);
    }

    private void Broadcast<T>(T packet,
        VcDeliveryMethod deliveryMethod = VcDeliveryMethod.Reliable,
        params VoiceCraftNetPeer[] excludes) where T : class, IVoiceCraftPacket
    {
        foreach (var server in _voiceCraftServers)
        {
            server.Broadcast(packet, deliveryMethod, excludes);
        }
    }

    private void BroadcastEvent<T>(T packet,
        VcDeliveryMethod deliveryMethod = VcDeliveryMethod.Reliable,
        params VoiceCraftNetPeer[] excludes) where T : class, IVoiceCraftEventPacket
    {
        foreach (var server in _voiceCraftServers)
        {
            //Each packet needs to be unique per server. Otherwise, we end up with duplicate in the pool.
            server.Broadcast(
                PacketPool<VcEventRequestPacket>
                    .GetPacket(() => new VcEventRequestPacket())
                    .Set(packet),
                deliveryMethod,
                excludes);
        }
    }

    private static void SendMcApi<T>(McApiNetPeer peer, T packet) where T : class, IMcApiPacket
    {
        peer.Server?.SendPacket(peer, packet);
    }

    private static void SendEventMcApi<T>(McApiNetPeer peer, T packet) where T : class, IMcApiEventPacket
    {
        peer.Server?.SendPacket(
            peer,
            PacketPool<McApiEventRequestPacket>
                .GetPacket(() => new McApiEventRequestPacket())
                .Set(packet));
    }

    private void BroadcastMcApi<T>(T packet) where T : class, IMcApiPacket
    {
        foreach (var server in _mcApiServers)
        {
            server.Broadcast(packet);
        }
    }

    private void BroadcastEventMcApi<T>(T packet) where T : class, IMcApiEventPacket
    {
        foreach (var server in _mcApiServers)
        {
            server.Broadcast(
                PacketPool<McApiEventRequestPacket>
                    .GetPacket(() => new McApiEventRequestPacket())
                    .Set(packet));
        }
    }

    #region Audio Effect Events

    private void OnAudioEffectSet(ushort bitmask, IAudioEffect? effect)
    {
        _tasks.Enqueue(() =>
        {
            BroadcastEvent(PacketPool<VcOnEffectUpdatedPacket>
                .GetPacket(() => new VcOnEffectUpdatedPacket())
                .Set(bitmask, effect));
            BroadcastEventMcApi(PacketPool<McApiOnEffectUpdatedPacket>
                .GetPacket(() => new McApiOnEffectUpdatedPacket())
                .Set(bitmask, effect));
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
        newEntity.OnPropertyUpdated += OnEntityPropertyUpdated;
        newEntity.OnVisibleEntityAdded += OnEntityVisibleEntityAdded;
        newEntity.OnVisibleEntityRemoved += OnEntityVisibleEntityRemoved;
        newEntity.OnAudioReceived += OnEntityAudioReceived;

        _tasks.Enqueue(() =>
        {
            if (newEntity is VoiceCraftNetworkEntity networkEntity)
            {
                Send(networkEntity.NetPeer,
                    PacketPool<VcSetNameRequestPacket>
                        .GetPacket(() => new VcSetNameRequestPacket())
                        .Set(networkEntity.Name));
                Send(networkEntity.NetPeer,
                    PacketPool<VcSetServerMuteRequestPacket>
                        .GetPacket(() => new VcSetServerMuteRequestPacket())
                        .Set(networkEntity.ServerMuted));
                Send(networkEntity.NetPeer,
                    PacketPool<VcSetServerDeafenRequestPacket>
                        .GetPacket(() => new VcSetServerDeafenRequestPacket())
                        .Set(networkEntity.ServerDeafened));
                BroadcastEvent(PacketPool<VcOnNetworkEntityCreatedPacket>
                        .GetPacket(() => new VcOnNetworkEntityCreatedPacket())
                        .Set(networkEntity),
                    VcDeliveryMethod.Reliable,
                    networkEntity.NetPeer);
                if (!EnableVisibilityDisplay)
                {
                    Broadcast(PacketPool<VcSetEntityVisibilityRequestPacket>
                            .GetPacket(() => new VcSetEntityVisibilityRequestPacket())
                            .Set(networkEntity.Id, true),
                        VcDeliveryMethod.Reliable, networkEntity.NetPeer);
                }

                BroadcastEventMcApi(PacketPool<McApiOnNetworkEntityCreatedPacket>
                    .GetPacket(() => new McApiOnNetworkEntityCreatedPacket())
                    .Set(networkEntity));

                //Send Effects
                var audioEffects = _audioEffectSystem.AudioEffects;
                foreach (var effect in audioEffects)
                    SendEvent(networkEntity.NetPeer,
                        PacketPool<VcOnEffectUpdatedPacket>
                            .GetPacket(() => new VcOnEffectUpdatedPacket())
                            .Set(effect.Key, effect.Value));

                //Send other entities.
                foreach (var entity in _world.Entities.Where(x => x != networkEntity))
                {
                    if (entity is VoiceCraftNetworkEntity otherNetworkEntity)
                        SendEvent(networkEntity.NetPeer,
                            PacketPool<VcOnNetworkEntityCreatedPacket>
                                .GetPacket(() => new VcOnNetworkEntityCreatedPacket())
                                .Set(otherNetworkEntity));
                    else
                        SendEvent(networkEntity.NetPeer,
                            PacketPool<VcOnEntityCreatedPacket>
                                .GetPacket(() => new VcOnEntityCreatedPacket())
                                .Set(entity));

                    if (EnableVisibilityDisplay) continue;
                    Send(networkEntity.NetPeer, PacketPool<VcSetEntityVisibilityRequestPacket>
                        .GetPacket(() => new VcSetEntityVisibilityRequestPacket())
                        .Set(entity.Id, true));
                }

                AnsiConsole.MarkupLine(
                    $"[green]{Localizer.Get($"Events.Client.Connected:{networkEntity.UserGuid}")}[/]");
            }
            else
            {
                BroadcastEvent(PacketPool<VcOnEntityCreatedPacket>
                    .GetPacket(() => new VcOnEntityCreatedPacket())
                    .Set(newEntity));
                BroadcastEventMcApi(PacketPool<McApiOnEntityCreatedPacket>
                    .GetPacket(() => new McApiOnEntityCreatedPacket())
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
        entity.OnPropertyUpdated -= OnEntityPropertyUpdated;
        entity.OnVisibleEntityAdded -= OnEntityVisibleEntityAdded;
        entity.OnVisibleEntityRemoved -= OnEntityVisibleEntityRemoved;
        entity.OnAudioReceived -= OnEntityAudioReceived;

        _tasks.Enqueue(() =>
        {
            if (entity is VoiceCraftNetworkEntity networkEntity)
            {
                Disconnect(networkEntity.NetPeer, "VoiceCraft.DisconnectReason.Kicked");
                AnsiConsole.MarkupLine(
                    $"[yellow]{Localizer.Get($"Events.Client.Disconnected:{networkEntity.UserGuid}")}[/]");
            }

            BroadcastEvent(PacketPool<VcOnEntityDestroyedPacket>
                .GetPacket(() => new VcOnEntityDestroyedPacket())
                .Set(entity.Id));
            BroadcastEventMcApi(PacketPool<McApiOnEntityDestroyedPacket>
                .GetPacket(() => new McApiOnEntityDestroyedPacket())
                .Set(entity.Id));
        });
    }

    private void OnMcApiPeerConnected(McApiNetPeer peer, string token)
    {
        _tasks.Enqueue(() =>
        {
            //Send Effects
            if (peer.SubscribedEvents.Contains(EventType.OnEffectUpdated))
            {
                var audioEffects = _audioEffectSystem.AudioEffects;
                foreach (var effect in audioEffects)
                    SendEventMcApi(peer, PacketPool<McApiOnEffectUpdatedPacket>
                        .GetPacket(() => new McApiOnEffectUpdatedPacket())
                        .Set(effect.Key, effect.Value));
            }

            //Send other entities.
            foreach (var entity in _world.Entities)
                if (entity is VoiceCraftNetworkEntity otherNetworkEntity &&
                    peer.SubscribedEvents.Contains(EventType.OnNetworkEntityCreated))
                    SendEventMcApi(peer,
                        PacketPool<McApiOnNetworkEntityCreatedPacket>
                            .GetPacket(() => new McApiOnNetworkEntityCreatedPacket())
                            .Set(otherNetworkEntity));
                else if (peer.SubscribedEvents.Contains(EventType.OnEntityCreated))
                    SendEventMcApi(peer,
                        PacketPool<McApiOnEntityCreatedPacket>
                            .GetPacket(() => new McApiOnEntityCreatedPacket())
                            .Set(entity));

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
            Send(entity.NetPeer,
                PacketPool<VcSetTitleRequestPacket>
                    .GetPacket(() => new VcSetTitleRequestPacket())
                    .Set(title));
        });
    }

    private void OnNetworkEntitySetDescription(string description, VoiceCraftNetworkEntity entity)
    {
        _tasks.Enqueue(() =>
        {
            Send(entity.NetPeer,
                PacketPool<VcSetDescriptionRequestPacket>
                    .GetPacket(() => new VcSetDescriptionRequestPacket())
                    .Set(description));
        });
    }

    private void OnNetworkEntityServerMuteUpdated(bool muted, VoiceCraftNetworkEntity entity)
    {
        _tasks.Enqueue(() =>
        {
            Send(entity.NetPeer,
                PacketPool<VcSetServerMuteRequestPacket>
                    .GetPacket(() => new VcSetServerMuteRequestPacket())
                    .Set(muted));

            BroadcastEvent(
                PacketPool<VcOnEntityServerMuteUpdatedPacket>
                    .GetPacket(() => new VcOnEntityServerMuteUpdatedPacket())
                    .Set(entity.Id, muted),
                VcDeliveryMethod.Reliable, entity.NetPeer);

            BroadcastEventMcApi(PacketPool<McApiOnEntityServerMuteUpdatedPacket>
                .GetPacket(() => new McApiOnEntityServerMuteUpdatedPacket())
                .Set(entity.Id, muted));
        });
    }

    private void OnNetworkEntityServerDeafenUpdated(bool deafened, VoiceCraftNetworkEntity entity)
    {
        _tasks.Enqueue(() =>
        {
            Send(entity.NetPeer,
                PacketPool<VcSetServerDeafenRequestPacket>
                    .GetPacket(() => new VcSetServerDeafenRequestPacket())
                    .Set(deafened));
            BroadcastEvent(
                PacketPool<VcOnEntityServerDeafenUpdatedPacket>
                    .GetPacket(() => new VcOnEntityServerDeafenUpdatedPacket())
                    .Set(entity.Id, deafened),
                VcDeliveryMethod.Reliable, entity.NetPeer);
            BroadcastEventMcApi(PacketPool<McApiOnEntityServerDeafenUpdatedPacket>
                .GetPacket(() => new McApiOnEntityServerDeafenUpdatedPacket())
                .Set(entity.Id, deafened));
        });
    }

    private void OnEntityWorldIdUpdated(string worldId, VoiceCraftEntity entity)
    {
        _tasks.Enqueue(() =>
        {
            BroadcastEventMcApi(PacketPool<McApiOnEntityWorldIdUpdatedPacket>
                .GetPacket(() => new McApiOnEntityWorldIdUpdatedPacket())
                .Set(entity.Id, worldId));
        });
    }

    private void OnEntityNameUpdated(string name, VoiceCraftEntity entity)
    {
        _tasks.Enqueue(() =>
        {
            var packet = PacketPool<VcOnEntityNameUpdatedPacket>
                .GetPacket(() => new VcOnEntityNameUpdatedPacket())
                .Set(entity.Id, name);
            if (entity is VoiceCraftNetworkEntity networkEntity)
            {
                if (networkEntity.PositioningType == PositioningType.Server)
                    Send(networkEntity.NetPeer,
                        PacketPool<VcSetNameRequestPacket>
                            .GetPacket(() => new VcSetNameRequestPacket())
                            .Set(name));

                BroadcastEvent(packet, VcDeliveryMethod.Reliable, networkEntity.NetPeer);
            }
            else
            {
                BroadcastEvent(packet);
            }

            BroadcastEventMcApi(PacketPool<McApiOnEntityNameUpdatedPacket>
                .GetPacket(() => new McApiOnEntityNameUpdatedPacket())
                .Set(entity.Id, name));
        });
    }

    private void OnEntityMuteUpdated(bool mute, VoiceCraftEntity entity)
    {
        _tasks.Enqueue(() =>
        {
            var packet = PacketPool<VcOnEntityMuteUpdatedPacket>
                .GetPacket(() => new VcOnEntityMuteUpdatedPacket())
                .Set(entity.Id, mute);

            if (entity is VoiceCraftNetworkEntity networkEntity)
                BroadcastEvent(packet, VcDeliveryMethod.Reliable, networkEntity.NetPeer);
            else
                BroadcastEvent(packet);

            BroadcastEventMcApi(PacketPool<McApiOnEntityMuteUpdatedPacket>
                .GetPacket(() => new McApiOnEntityMuteUpdatedPacket())
                .Set(entity.Id, mute));
        });
    }

    private void OnEntityDeafenUpdated(bool deafen, VoiceCraftEntity entity)
    {
        _tasks.Enqueue(() =>
        {
            var packet = PacketPool<VcOnEntityDeafenUpdatedPacket>
                .GetPacket(() => new VcOnEntityDeafenUpdatedPacket())
                .Set(entity.Id, deafen);
            if (entity is VoiceCraftNetworkEntity networkEntity)
                BroadcastEvent(packet, VcDeliveryMethod.Reliable, networkEntity.NetPeer);
            else
                BroadcastEvent(packet);

            BroadcastEventMcApi(PacketPool<McApiOnEntityDeafenUpdatedPacket>
                .GetPacket(() => new McApiOnEntityDeafenUpdatedPacket())
                .Set(entity.Id, deafen));
        });
    }

    private void OnEntityTalkBitmaskUpdated(ushort bitmask, VoiceCraftEntity entity)
    {
        _tasks.Enqueue(() =>
        {
            var packet = PacketPool<VcOnEntityTalkBitmaskUpdatedPacket>
                .GetPacket(() => new VcOnEntityTalkBitmaskUpdatedPacket())
                .Set(entity.Id, bitmask);
            if (entity is VoiceCraftNetworkEntity networkEntity)
            {
                Send(networkEntity.NetPeer,
                    PacketPool<VcSetTalkBitmaskRequestPacket>
                        .GetPacket(() => new VcSetTalkBitmaskRequestPacket())
                        .Set(bitmask));
                BroadcastEvent(packet, VcDeliveryMethod.Reliable, networkEntity.NetPeer);
            }
            else
            {
                BroadcastEvent(packet);
            }

            BroadcastEventMcApi(PacketPool<McApiOnEntityTalkBitmaskUpdatedPacket>
                .GetPacket(() => new McApiOnEntityTalkBitmaskUpdatedPacket())
                .Set(entity.Id, bitmask));
        });
    }

    private void OnEntityListenBitmaskUpdated(ushort bitmask, VoiceCraftEntity entity)
    {
        _tasks.Enqueue(() =>
        {
            var packet = PacketPool<VcOnEntityListenBitmaskUpdatedPacket>
                .GetPacket(() => new VcOnEntityListenBitmaskUpdatedPacket())
                .Set(entity.Id, bitmask);
            if (entity is VoiceCraftNetworkEntity networkEntity)
            {
                Send(networkEntity.NetPeer,
                    PacketPool<VcSetListenBitmaskRequestPacket>
                        .GetPacket(() => new VcSetListenBitmaskRequestPacket())
                        .Set(bitmask));
                BroadcastEvent(packet, VcDeliveryMethod.Reliable, networkEntity.NetPeer);
            }
            else
            {
                BroadcastEvent(packet);
            }

            BroadcastEventMcApi(
                PacketPool<McApiOnEntityListenBitmaskUpdatedPacket>
                    .GetPacket(() => new McApiOnEntityListenBitmaskUpdatedPacket())
                    .Set(entity.Id, bitmask));
        });
    }

    private void OnEntityEffectBitmaskUpdated(ushort bitmask, VoiceCraftEntity entity)
    {
        _tasks.Enqueue(() =>
        {
            var packet = PacketPool<VcOnEntityEffectBitmaskUpdatedPacket>
                .GetPacket(() => new VcOnEntityEffectBitmaskUpdatedPacket())
                .Set(entity.Id, bitmask);
            if (entity is VoiceCraftNetworkEntity networkEntity)
            {
                Send(networkEntity.NetPeer,
                    PacketPool<VcSetEffectBitmaskRequestPacket>
                        .GetPacket(() => new VcSetEffectBitmaskRequestPacket())
                        .Set(bitmask));
                BroadcastEvent(packet, VcDeliveryMethod.Reliable, networkEntity.NetPeer);
            }
            else
            {
                BroadcastEvent(packet);
            }

            BroadcastEventMcApi(PacketPool<McApiOnEntityEffectBitmaskUpdatedPacket>
                .GetPacket(() => new McApiOnEntityEffectBitmaskUpdatedPacket())
                .Set(entity.Id, bitmask));
        });
    }

//Properties
    private void OnEntityPositionUpdated(Vector3 position, VoiceCraftEntity entity)
    {
        _tasks.Enqueue(() =>
        {
            if (entity is VoiceCraftNetworkEntity { PositioningType: PositioningType.Server } networkEntity)
                Send(networkEntity.NetPeer,
                    PacketPool<VcSetPositionRequestPacket>
                        .GetPacket(() => new VcSetPositionRequestPacket())
                        .Set(position));

            foreach (var ve in entity.VisibleEntities)
            {
                if (ve is not VoiceCraftNetworkEntity visibleEntity) continue;
                SendEvent(visibleEntity.NetPeer,
                    PacketPool<VcOnEntityPositionUpdatedPacket>
                        .GetPacket(() => new VcOnEntityPositionUpdatedPacket())
                        .Set(entity.Id, position));
            }

            BroadcastEventMcApi(PacketPool<McApiOnEntityPositionUpdatedPacket>
                .GetPacket(() => new McApiOnEntityPositionUpdatedPacket())
                .Set(entity.Id, position));
        });
    }

    private void OnEntityRotationUpdated(Vector2 rotation, VoiceCraftEntity entity)
    {
        _tasks.Enqueue(() =>
        {
            if (entity is VoiceCraftNetworkEntity { PositioningType: PositioningType.Server } networkEntity)
                Send(networkEntity.NetPeer,
                    PacketPool<VcSetRotationRequestPacket>
                        .GetPacket(() => new VcSetRotationRequestPacket())
                        .Set(rotation));

            foreach (var ve in entity.VisibleEntities)
            {
                if (ve is not VoiceCraftNetworkEntity visibleEntity) continue;
                SendEvent(visibleEntity.NetPeer,
                    PacketPool<VcOnEntityRotationUpdatedPacket>
                        .GetPacket(() => new VcOnEntityRotationUpdatedPacket())
                        .Set(entity.Id, rotation));
            }

            BroadcastEventMcApi(PacketPool<McApiOnEntityRotationUpdatedPacket>
                .GetPacket(() => new McApiOnEntityRotationUpdatedPacket())
                .Set(entity.Id, rotation));
        });
    }

    private void OnEntityPropertyUpdated(string key, object? value, VoiceCraftEntity entity)
    {
        _tasks.Enqueue(() =>
        {
            var packet = PacketPool<VcOnEntityPropertyUpdatedPacket>
                .GetPacket(() => new VcOnEntityPropertyUpdatedPacket()).Set(entity.Id, key, value);
            if (entity is VoiceCraftNetworkEntity networkEntity)
            {
                Send(networkEntity.NetPeer,
                    PacketPool<VcSetPropertyRequestPacket>
                        .GetPacket(() => new VcSetPropertyRequestPacket())
                        .Set(key, value));
                BroadcastEvent(packet, VcDeliveryMethod.Reliable, networkEntity.NetPeer);
            }
            else
            {
                BroadcastEvent(packet);
            }
            
            BroadcastEventMcApi(PacketPool<McApiOnEntityPropertyUpdatedPacket>
                .GetPacket(() => new McApiOnEntityPropertyUpdatedPacket())
                .Set(entity.Id, key, value));
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
                    Send(networkEntity.NetPeer, visibilityPacket);
                }

                //Position
                SendEvent(networkEntity.NetPeer,
                    PacketPool<VcOnEntityPositionUpdatedPacket>
                        .GetPacket(() => new VcOnEntityPositionUpdatedPacket())
                        .Set(entity.Id, entity.Position));

                //Rotation
                SendEvent(networkEntity.NetPeer,
                    PacketPool<VcOnEntityRotationUpdatedPacket>
                        .GetPacket(() => new VcOnEntityRotationUpdatedPacket())
                        .Set(entity.Id, entity.Rotation));
            }

            BroadcastEventMcApi(PacketPool<McApiOnEntityVisibilityUpdatedPacket>
                .GetPacket(() => new McApiOnEntityVisibilityUpdatedPacket())
                .Set(entity.Id, addedEntity.Id, true));
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
                    Send(networkEntity.NetPeer,
                        PacketPool<VcSetEntityVisibilityRequestPacket>
                            .GetPacket(() => new VcSetEntityVisibilityRequestPacket())
                            .Set(entity.Id));
                }
            }

            BroadcastEventMcApi(PacketPool<McApiOnEntityVisibilityUpdatedPacket>
                .GetPacket(() => new McApiOnEntityVisibilityUpdatedPacket())
                .Set(entity.Id, removedEntity.Id));
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
                SendEvent(visibleEntity.NetPeer,
                    PacketPool<VcOnEntityAudioDataReceivedPacket>
                        .GetPacket(() => new VcOnEntityAudioDataReceivedPacket())
                        .Set(entity.Id, timestamp, frameLoudness, buffer.Length, buffer),
                    VcDeliveryMethod.Unreliable);
            }

            BroadcastEventMcApi(PacketPool<McApiOnEntityAudioReceivedPacket>
                .GetPacket(() => new McApiOnEntityAudioReceivedPacket())
                .Set(entity.Id, timestamp, frameLoudness));
        });
    }

    #endregion
}