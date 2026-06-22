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
        var eventPacket = PacketPool<VcEventRequestPacket>.GetPacket(() => new VcEventRequestPacket());
        try
        {
            eventPacket.Set(packet);
            peer.Server?.SendPacket(peer, eventPacket, deliveryMethod);
        }
        finally
        {
            eventPacket.Set(null);
            eventPacket.Return();
        }
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
        var eventPacket = PacketPool<VcEventRequestPacket>.GetPacket(() => new VcEventRequestPacket());
        try
        {
            eventPacket.Set(packet);
            foreach (var server in _voiceCraftServers)
            {
                server.Broadcast(eventPacket, deliveryMethod, excludes);
            }
        }
        finally
        {
            eventPacket.Set(null);
            eventPacket.Return();
        }
    }

    private static void SendMcApi<T>(McApiNetPeer peer, T packet) where T : class, IMcApiPacket
    {
        peer.Server?.SendPacket(peer, packet);
    }

    private static void SendEventMcApi<T>(McApiNetPeer peer, T packet) where T : class, IMcApiEventPacket
    {
        if (!peer.SubscribedEvents.Contains(packet.EventType)) return;
        var eventPacket = PacketPool<McApiEventRequestPacket>.GetPacket(() => new McApiEventRequestPacket());
        try
        {
            eventPacket.Set(packet);
            peer.Server?.SendPacket(peer, eventPacket);
        }
        finally
        {
            eventPacket.Set(null); //Set to null before returning.
            eventPacket.Return();
        }
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
        var eventPacket = PacketPool<McApiEventRequestPacket>.GetPacket(() => new McApiEventRequestPacket());
        try
        {
            eventPacket.Set(packet);
            foreach (var server in _mcApiServers)
            {
                foreach (var peer in server.Peers.Where(x =>
                             x.SubscribedEvents.Contains(packet.EventType) &&
                             x.ConnectionState == McApiConnectionState.Connected))
                {
                    server.SendPacket(peer, eventPacket);
                }
            }
        }
        finally
        {
            eventPacket.Set(null); //Set to null before returning.
            eventPacket.Return();
        }
    }

    #region Audio Effect Events

    private void OnAudioEffectSet(ushort bitmask, IAudioEffect? effect)
    {
        _tasks.Enqueue(() =>
        {
            var vcPacket = PacketPool<VcOnEffectUpdatedPacket>
                .GetPacket(() => new VcOnEffectUpdatedPacket());
            var mcApiPacket = PacketPool<McApiOnEffectUpdatedPacket>
                .GetPacket(() => new McApiOnEffectUpdatedPacket());
            try
            {
                vcPacket.Set(bitmask, effect);
                mcApiPacket.Set(bitmask, effect);

                BroadcastEvent(vcPacket);
                BroadcastEventMcApi(mcApiPacket);
            }
            finally
            {
                vcPacket.Return();
                mcApiPacket.Return();
            }
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
            var setNamePacket = PacketPool<VcSetNameRequestPacket>
                .GetPacket(() => new VcSetNameRequestPacket());
            var setServerMutePacket = PacketPool<VcSetServerMuteRequestPacket>
                .GetPacket(() => new VcSetServerMuteRequestPacket());
            var setServerDeafenPacket = PacketPool<VcSetServerDeafenRequestPacket>
                .GetPacket(() => new VcSetServerDeafenRequestPacket());
            var setEntityVisibilityPacket = PacketPool<VcSetEntityVisibilityRequestPacket>
                .GetPacket(() => new VcSetEntityVisibilityRequestPacket());
            var onNetworkEntityCreatedPacket = PacketPool<VcOnNetworkEntityCreatedPacket>
                .GetPacket(() => new VcOnNetworkEntityCreatedPacket());
            var onEffectUpdatedPacket = PacketPool<VcOnEffectUpdatedPacket>
                .GetPacket(() => new VcOnEffectUpdatedPacket());
            var onEntityCreatedPacket = PacketPool<VcOnEntityCreatedPacket>
                .GetPacket(() => new VcOnEntityCreatedPacket());
            var mcApiOnNetworkEntityCreatedPacket = PacketPool<McApiOnNetworkEntityCreatedPacket>
                .GetPacket(() => new McApiOnNetworkEntityCreatedPacket());
            var mcApiOnEntityCreatedPacket = PacketPool<McApiOnEntityCreatedPacket>
                .GetPacket(() => new McApiOnEntityCreatedPacket());

            try
            {
                if (newEntity is VoiceCraftNetworkEntity networkEntity)
                {
                    setNamePacket.Set(networkEntity.Name);
                    setServerMutePacket.Set(networkEntity.ServerMuted);
                    setServerDeafenPacket.Set(networkEntity.ServerDeafened);
                    onNetworkEntityCreatedPacket.Set(networkEntity);

                    Send(networkEntity.NetPeer, setNamePacket);
                    Send(networkEntity.NetPeer, setServerMutePacket);
                    Send(networkEntity.NetPeer, setServerDeafenPacket);
                    BroadcastEvent(onNetworkEntityCreatedPacket, VcDeliveryMethod.Reliable, networkEntity.NetPeer);
                    if (!EnableVisibilityDisplay)
                    {
                        setEntityVisibilityPacket.Set(networkEntity.Id, true);
                        Broadcast(setEntityVisibilityPacket, VcDeliveryMethod.Reliable, networkEntity.NetPeer);
                    }

                    mcApiOnNetworkEntityCreatedPacket.Set(networkEntity);
                    BroadcastEventMcApi(mcApiOnNetworkEntityCreatedPacket);

                    //Send Effects
                    var audioEffects = _audioEffectSystem.AudioEffects;
                    foreach (var effect in audioEffects)
                    {
                        onEffectUpdatedPacket.Set(effect.Key, effect.Value);
                        SendEvent(networkEntity.NetPeer, onEffectUpdatedPacket);
                    }

                    //Send other entities.
                    foreach (var entity in _world.Entities.Where(x => x != networkEntity))
                    {
                        if (entity is VoiceCraftNetworkEntity otherNetworkEntity)
                        {
                            onNetworkEntityCreatedPacket.Set(otherNetworkEntity);
                            SendEvent(networkEntity.NetPeer, onNetworkEntityCreatedPacket);
                        }
                        else
                        {
                            onEntityCreatedPacket.Set(entity);
                            SendEvent(networkEntity.NetPeer, onEntityCreatedPacket);
                        }

                        if (EnableVisibilityDisplay) continue;
                        setEntityVisibilityPacket.Set(entity.Id, true);
                        Send(networkEntity.NetPeer, setEntityVisibilityPacket);
                    }

                    AnsiConsole.MarkupLine(
                        $"[green]{Localizer.Get($"Events.Client.Connected:{networkEntity.UserGuid}")}[/]");
                }
                else
                {
                    onEntityCreatedPacket.Set(newEntity);
                    mcApiOnEntityCreatedPacket.Set(newEntity);

                    BroadcastEvent(onEntityCreatedPacket);
                    BroadcastEventMcApi(mcApiOnEntityCreatedPacket);
                }
            }
            finally
            {
                setNamePacket.Return();
                setServerMutePacket.Return();
                setServerDeafenPacket.Return();
                setEntityVisibilityPacket.Return();
                onNetworkEntityCreatedPacket.Return();
                onEffectUpdatedPacket.Return();
                onEntityCreatedPacket.Return();
                mcApiOnNetworkEntityCreatedPacket.Return();
                mcApiOnEntityCreatedPacket.Return();
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
            var vcPacket = PacketPool<VcOnEntityDestroyedPacket>
                .GetPacket(() => new VcOnEntityDestroyedPacket());
            var mcApiPacket = PacketPool<McApiOnEntityDestroyedPacket>
                .GetPacket(() => new McApiOnEntityDestroyedPacket());

            try
            {
                if (entity is VoiceCraftNetworkEntity networkEntity)
                {
                    Disconnect(networkEntity.NetPeer, "VoiceCraft.DisconnectReason.Kicked");
                    AnsiConsole.MarkupLine(
                        $"[yellow]{Localizer.Get($"Events.Client.Disconnected:{networkEntity.UserGuid}")}[/]");
                }

                vcPacket.Set(entity.Id);
                mcApiPacket.Set(entity.Id);

                BroadcastEvent(vcPacket);
                BroadcastEventMcApi(mcApiPacket);
            }
            finally
            {
                vcPacket.Return();
                mcApiPacket.Return();
            }
        });
    }

    private void OnMcApiPeerConnected(McApiNetPeer peer, string token)
    {
        _tasks.Enqueue(() =>
        {
            var onEffectUpdatedPacket = PacketPool<McApiOnEffectUpdatedPacket>
                .GetPacket(() => new McApiOnEffectUpdatedPacket());
            var onNetworkEntityCreatedPacket = PacketPool<McApiOnNetworkEntityCreatedPacket>
                .GetPacket(() => new McApiOnNetworkEntityCreatedPacket());
            var onEntityCreatedPacket = PacketPool<McApiOnEntityCreatedPacket>
                .GetPacket(() => new McApiOnEntityCreatedPacket());

            try
            {
                //Send Effects
                if (peer.SubscribedEvents.Contains(EventType.OnEffectUpdated))
                {
                    var audioEffects = _audioEffectSystem.AudioEffects;
                    foreach (var effect in audioEffects)
                    {
                        onEffectUpdatedPacket.Set(effect.Key, effect.Value);
                        SendEventMcApi(peer, onEffectUpdatedPacket);
                    }
                }

                //Send other entities.
                foreach (var entity in _world.Entities)
                    if (entity is VoiceCraftNetworkEntity networkEntity &&
                        peer.SubscribedEvents.Contains(EventType.OnNetworkEntityCreated))
                    {
                        onNetworkEntityCreatedPacket.Set(networkEntity);
                        SendEventMcApi(peer, onNetworkEntityCreatedPacket);
                    }
                    else if (peer.SubscribedEvents.Contains(EventType.OnEntityCreated))
                    {
                        onEntityCreatedPacket.Set(entity);
                        SendEventMcApi(peer, onEntityCreatedPacket);
                    }

                AnsiConsole.MarkupLine($"[green]{Localizer.Get($"Events.McApi.Client.Connected:{token}")}[/]");
            }
            finally
            {
                onEffectUpdatedPacket.Return();
                onNetworkEntityCreatedPacket.Return();
                onEntityCreatedPacket.Return();
            }
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
            var packet = PacketPool<VcSetTitleRequestPacket>.GetPacket(() => new VcSetTitleRequestPacket());
            try
            {
                packet.Set(title);
                Send(entity.NetPeer, packet);
            }
            finally
            {
                packet.Return();
            }
        });
    }

    private void OnNetworkEntitySetDescription(string description, VoiceCraftNetworkEntity entity)
    {
        _tasks.Enqueue(() =>
        {
            var packet = PacketPool<VcSetDescriptionRequestPacket>.GetPacket(() => new VcSetDescriptionRequestPacket());
            try
            {
                packet.Set(description);
                Send(entity.NetPeer, packet);
            }
            finally
            {
                packet.Return();
            }
        });
    }

    private void OnNetworkEntityServerMuteUpdated(bool muted, VoiceCraftNetworkEntity entity)
    {
        _tasks.Enqueue(() =>
        {
            var setServerMutePacket = PacketPool<VcSetServerMuteRequestPacket>
                .GetPacket(() => new VcSetServerMuteRequestPacket());
            var vcPacket = PacketPool<VcOnEntityServerMuteUpdatedPacket>
                .GetPacket(() => new VcOnEntityServerMuteUpdatedPacket());
            var mcApiPacket = PacketPool<McApiOnEntityServerMuteUpdatedPacket>
                .GetPacket(() => new McApiOnEntityServerMuteUpdatedPacket());

            try
            {
                setServerMutePacket.Set(muted);
                vcPacket.Set(entity.Id, muted);
                mcApiPacket.Set(entity.Id, muted);

                Send(entity.NetPeer, setServerMutePacket);
                BroadcastEvent(vcPacket, VcDeliveryMethod.Reliable, entity.NetPeer);
                BroadcastEventMcApi(mcApiPacket);
            }
            finally
            {
                setServerMutePacket.Return();
                vcPacket.Return();
                mcApiPacket.Return();
            }
        });
    }

    private void OnNetworkEntityServerDeafenUpdated(bool deafened, VoiceCraftNetworkEntity entity)
    {
        _tasks.Enqueue(() =>
        {
            var setServerDeafenPacket = PacketPool<VcSetServerDeafenRequestPacket>
                .GetPacket(() => new VcSetServerDeafenRequestPacket());
            var vcPacket = PacketPool<VcOnEntityServerDeafenUpdatedPacket>
                .GetPacket(() => new VcOnEntityServerDeafenUpdatedPacket());
            var mcApiPacket = PacketPool<McApiOnEntityServerDeafenUpdatedPacket>
                .GetPacket(() => new McApiOnEntityServerDeafenUpdatedPacket());
            try
            {
                setServerDeafenPacket.Set(deafened);
                vcPacket.Set(entity.Id, deafened);
                mcApiPacket.Set(entity.Id, deafened);

                Send(entity.NetPeer, setServerDeafenPacket);
                BroadcastEvent(vcPacket, VcDeliveryMethod.Reliable, entity.NetPeer);
                BroadcastEventMcApi(mcApiPacket);
            }
            finally
            {
                setServerDeafenPacket.Return();
                vcPacket.Return();
                mcApiPacket.Return();
            }
        });
    }

    private void OnEntityWorldIdUpdated(string worldId, VoiceCraftEntity entity)
    {
        _tasks.Enqueue(() =>
        {
            var packet = PacketPool<McApiOnEntityWorldIdUpdatedPacket>
                .GetPacket(() => new McApiOnEntityWorldIdUpdatedPacket());

            try
            {
                packet.Set(entity.Id, worldId);
                BroadcastEventMcApi(packet);
            }
            finally
            {
                packet.Return();
            }
        });
    }

    private void OnEntityNameUpdated(string name, VoiceCraftEntity entity)
    {
        _tasks.Enqueue(() =>
        {
            var setNamePacket = PacketPool<VcSetNameRequestPacket>
                .GetPacket(() => new VcSetNameRequestPacket());
            var vcPacket = PacketPool<VcOnEntityNameUpdatedPacket>
                .GetPacket(() => new VcOnEntityNameUpdatedPacket());
            var mcApiPacket = PacketPool<McApiOnEntityNameUpdatedPacket>
                .GetPacket(() => new McApiOnEntityNameUpdatedPacket());

            try
            {
                vcPacket.Set(entity.Id, name);
                mcApiPacket.Set(entity.Id, name);

                if (entity is VoiceCraftNetworkEntity networkEntity)
                {
                    if (networkEntity.PositioningType == PositioningType.Server)
                    {
                        setNamePacket.Set(name);
                        Send(networkEntity.NetPeer, setNamePacket);
                    }

                    BroadcastEvent(vcPacket, VcDeliveryMethod.Reliable, networkEntity.NetPeer);
                }
                else
                {
                    BroadcastEvent(vcPacket);
                }

                BroadcastEventMcApi(mcApiPacket);
            }
            finally
            {
                setNamePacket.Return();
                vcPacket.Return();
                mcApiPacket.Return();
            }
        });
    }

    private void OnEntityMuteUpdated(bool mute, VoiceCraftEntity entity)
    {
        _tasks.Enqueue(() =>
        {
            var vcPacket = PacketPool<VcOnEntityMuteUpdatedPacket>
                .GetPacket(() => new VcOnEntityMuteUpdatedPacket());
            var mcApiPacket = PacketPool<McApiOnEntityMuteUpdatedPacket>
                .GetPacket(() => new McApiOnEntityMuteUpdatedPacket());

            try
            {
                vcPacket.Set(entity.Id, mute);
                mcApiPacket.Set(entity.Id, mute);

                if (entity is VoiceCraftNetworkEntity networkEntity)
                    BroadcastEvent(vcPacket, VcDeliveryMethod.Reliable, networkEntity.NetPeer);
                else
                    BroadcastEvent(vcPacket);

                BroadcastEventMcApi(mcApiPacket);
            }
            finally
            {
                vcPacket.Return();
                mcApiPacket.Return();
            }
        });
    }

    private void OnEntityDeafenUpdated(bool deafen, VoiceCraftEntity entity)
    {
        _tasks.Enqueue(() =>
        {
            var vcPacket = PacketPool<VcOnEntityDeafenUpdatedPacket>
                .GetPacket(() => new VcOnEntityDeafenUpdatedPacket());
            var mcApiPacket = PacketPool<McApiOnEntityDeafenUpdatedPacket>
                .GetPacket(() => new McApiOnEntityDeafenUpdatedPacket());

            try
            {
                vcPacket.Set(entity.Id, deafen);
                mcApiPacket.Set(entity.Id, deafen);

                if (entity is VoiceCraftNetworkEntity networkEntity)
                    BroadcastEvent(vcPacket, VcDeliveryMethod.Reliable, networkEntity.NetPeer);
                else
                    BroadcastEvent(vcPacket);

                BroadcastEventMcApi(mcApiPacket);
            }
            finally
            {
                vcPacket.Return();
                mcApiPacket.Return();
            }
        });
    }

    private void OnEntityTalkBitmaskUpdated(ushort bitmask, VoiceCraftEntity entity)
    {
        _tasks.Enqueue(() =>
        {
            var setTalkBitmaskPacket = PacketPool<VcSetTalkBitmaskRequestPacket>
                .GetPacket(() => new VcSetTalkBitmaskRequestPacket());
            var vcPacket = PacketPool<VcOnEntityTalkBitmaskUpdatedPacket>
                .GetPacket(() => new VcOnEntityTalkBitmaskUpdatedPacket());
            var mcApiPacket = PacketPool<McApiOnEntityTalkBitmaskUpdatedPacket>
                .GetPacket(() => new McApiOnEntityTalkBitmaskUpdatedPacket());

            try
            {
                vcPacket.Set(entity.Id, bitmask);
                mcApiPacket.Set(entity.Id, bitmask);

                if (entity is VoiceCraftNetworkEntity networkEntity)
                {
                    if (networkEntity.PositioningType == PositioningType.Server)
                    {
                        setTalkBitmaskPacket.Set(bitmask);
                        Send(networkEntity.NetPeer, setTalkBitmaskPacket);
                    }

                    BroadcastEvent(vcPacket, VcDeliveryMethod.Reliable, networkEntity.NetPeer);
                }
                else
                {
                    BroadcastEvent(vcPacket);
                }

                BroadcastEventMcApi(mcApiPacket);
            }
            finally
            {
                setTalkBitmaskPacket.Return();
                vcPacket.Return();
                mcApiPacket.Return();
            }
        });
    }

    private void OnEntityListenBitmaskUpdated(ushort bitmask, VoiceCraftEntity entity)
    {
        _tasks.Enqueue(() =>
        {
            var setListenBitmaskPacket = PacketPool<VcSetListenBitmaskRequestPacket>
                .GetPacket(() => new VcSetListenBitmaskRequestPacket());
            var vcPacket = PacketPool<VcOnEntityListenBitmaskUpdatedPacket>
                .GetPacket(() => new VcOnEntityListenBitmaskUpdatedPacket());
            var mcApiPacket = PacketPool<McApiOnEntityListenBitmaskUpdatedPacket>
                .GetPacket(() => new McApiOnEntityListenBitmaskUpdatedPacket());

            try
            {
                vcPacket.Set(entity.Id, bitmask);
                mcApiPacket.Set(entity.Id, bitmask);

                if (entity is VoiceCraftNetworkEntity networkEntity)
                {
                    if (networkEntity.PositioningType == PositioningType.Server)
                    {
                        setListenBitmaskPacket.Set(bitmask);
                        Send(networkEntity.NetPeer, setListenBitmaskPacket);
                    }

                    BroadcastEvent(vcPacket, VcDeliveryMethod.Reliable, networkEntity.NetPeer);
                }
                else
                {
                    BroadcastEvent(vcPacket);
                }

                BroadcastEventMcApi(mcApiPacket);
            }
            finally
            {
                setListenBitmaskPacket.Return();
                vcPacket.Return();
                mcApiPacket.Return();
            }
        });
    }

    private void OnEntityEffectBitmaskUpdated(ushort bitmask, VoiceCraftEntity entity)
    {
        _tasks.Enqueue(() =>
        {
            var setEffectBitmaskPacket = PacketPool<VcSetEffectBitmaskRequestPacket>
                .GetPacket(() => new VcSetEffectBitmaskRequestPacket());
            var vcPacket = PacketPool<VcOnEntityEffectBitmaskUpdatedPacket>
                .GetPacket(() => new VcOnEntityEffectBitmaskUpdatedPacket());
            var mcApiPacket = PacketPool<McApiOnEntityEffectBitmaskUpdatedPacket>
                .GetPacket(() => new McApiOnEntityEffectBitmaskUpdatedPacket());

            try
            {
                vcPacket.Set(entity.Id, bitmask);
                mcApiPacket.Set(entity.Id, bitmask);

                if (entity is VoiceCraftNetworkEntity networkEntity)
                {
                    if (networkEntity.PositioningType == PositioningType.Server)
                    {
                        setEffectBitmaskPacket.Set(bitmask);
                        Send(networkEntity.NetPeer, setEffectBitmaskPacket);
                    }

                    BroadcastEvent(vcPacket, VcDeliveryMethod.Reliable, networkEntity.NetPeer);
                }
                else
                {
                    BroadcastEvent(vcPacket);
                }

                BroadcastEventMcApi(mcApiPacket);
            }
            finally
            {
                setEffectBitmaskPacket.Return();
                vcPacket.Return();
                mcApiPacket.Return();
            }
        });
    }

    //Properties
    private void OnEntityPositionUpdated(Vector3 position, VoiceCraftEntity entity)
    {
        _tasks.Enqueue(() =>
        {
            var setPositionPacket = PacketPool<VcSetPositionRequestPacket>
                .GetPacket(() => new VcSetPositionRequestPacket());
            var vcPacket = PacketPool<VcOnEntityPositionUpdatedPacket>
                .GetPacket(() => new VcOnEntityPositionUpdatedPacket());
            var mcApiPacket = PacketPool<McApiOnEntityPositionUpdatedPacket>
                .GetPacket(() => new McApiOnEntityPositionUpdatedPacket());

            try
            {
                if (entity is VoiceCraftNetworkEntity { PositioningType: PositioningType.Server } networkEntity)
                {
                    setPositionPacket.Set(position);
                    Send(networkEntity.NetPeer, setPositionPacket);
                }

                foreach (var ve in entity.VisibleEntities)
                {
                    if (ve is not VoiceCraftNetworkEntity visibleEntity) continue;
                    vcPacket.Set(visibleEntity.Id, position);
                    SendEvent(visibleEntity.NetPeer, vcPacket);
                }

                mcApiPacket.Set(entity.Id, position);
                BroadcastEventMcApi(mcApiPacket);
            }
            finally
            {
                setPositionPacket.Return();
                vcPacket.Return();
                mcApiPacket.Return();
            }
        });
    }

    private void OnEntityRotationUpdated(Vector2 rotation, VoiceCraftEntity entity)
    {
        _tasks.Enqueue(() =>
        {
            var setRotationPacket = PacketPool<VcSetRotationRequestPacket>
                .GetPacket(() => new VcSetRotationRequestPacket());
            var vcPacket = PacketPool<VcOnEntityRotationUpdatedPacket>
                .GetPacket(() => new VcOnEntityRotationUpdatedPacket());
            var mcApiPacket = PacketPool<McApiOnEntityRotationUpdatedPacket>
                .GetPacket(() => new McApiOnEntityRotationUpdatedPacket());

            try
            {
                if (entity is VoiceCraftNetworkEntity { PositioningType: PositioningType.Server } networkEntity)
                {
                    setRotationPacket.Set(rotation);
                    Send(networkEntity.NetPeer, setRotationPacket);
                }

                foreach (var ve in entity.VisibleEntities)
                {
                    if (ve is not VoiceCraftNetworkEntity visibleEntity) continue;
                    vcPacket.Set(visibleEntity.Id, rotation);
                    SendEvent(visibleEntity.NetPeer, vcPacket);
                }

                mcApiPacket.Set(entity.Id, rotation);
                BroadcastEventMcApi(mcApiPacket);
            }
            finally
            {
                setRotationPacket.Return();
                vcPacket.Return();
                mcApiPacket.Return();
            }
        });
    }

    private void OnEntityPropertyUpdated(string key, object? value, VoiceCraftEntity entity)
    {
        _tasks.Enqueue(() =>
        {
            var setPropertyPacket = PacketPool<VcSetPropertyRequestPacket>
                .GetPacket(() => new VcSetPropertyRequestPacket());
            var vcPacket = PacketPool<VcOnEntityPropertyUpdatedPacket>
                .GetPacket(() => new VcOnEntityPropertyUpdatedPacket());
            var mcApiPacket = PacketPool<McApiOnEntityPropertyUpdatedPacket>
                .GetPacket(() => new McApiOnEntityPropertyUpdatedPacket());

            try
            {
                vcPacket.Set(entity.Id, key, value);
                mcApiPacket.Set(entity.Id, key, value);

                if (entity is VoiceCraftNetworkEntity networkEntity)
                {
                    if (networkEntity.PositioningType == PositioningType.Server)
                    {
                        setPropertyPacket.Set(key, value);
                        Send(networkEntity.NetPeer, setPropertyPacket);
                    }

                    BroadcastEvent(vcPacket, VcDeliveryMethod.Reliable, networkEntity.NetPeer);
                }
                else
                {
                    BroadcastEvent(vcPacket);
                }

                BroadcastEventMcApi(mcApiPacket);
            }
            finally
            {
                setPropertyPacket.Return();
                vcPacket.Return();
                mcApiPacket.Return();
            }
        });
    }

    //TODO FINISH THIS!
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