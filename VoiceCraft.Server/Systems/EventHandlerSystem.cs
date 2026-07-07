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
            eventPacket.Set(null); //Set to null before returning.
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
            eventPacket.Set(null); //Set to null before returning.
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
                BroadcastEvent(vcPacket);

                mcApiPacket.Set(bitmask, effect);
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
            //Effect Sync
            var onEffectUpdatedPacket = PacketPool<VcOnEffectUpdatedPacket>
                .GetPacket(() => new VcOnEffectUpdatedPacket());

            //Entity Sync
            var onNetworkEntityCreatedPacket = PacketPool<VcOnNetworkEntityCreatedPacket>
                .GetPacket(() => new VcOnNetworkEntityCreatedPacket());
            var onEntityServerMuteUpdatedPacket = PacketPool<VcOnEntityServerMuteUpdatedPacket>
                .GetPacket(() => new VcOnEntityServerMuteUpdatedPacket());
            var onEntityServerDeafenUpdatedPacket = PacketPool<VcOnEntityDeafenUpdatedPacket>
                .GetPacket(() => new VcOnEntityDeafenUpdatedPacket());
            var onEntityCreatedPacket = PacketPool<VcOnEntityCreatedPacket>
                .GetPacket(() => new VcOnEntityCreatedPacket());
            var onEntityNameUpdatedPacket = PacketPool<VcOnEntityNameUpdatedPacket>
                .GetPacket(() => new VcOnEntityNameUpdatedPacket());
            var onEntityMuteUpdatedPacket = PacketPool<VcOnEntityMuteUpdatedPacket>
                .GetPacket(() => new VcOnEntityMuteUpdatedPacket());
            var onEntityDeafenUpdatedPacket = PacketPool<VcOnEntityDeafenUpdatedPacket>
                .GetPacket(() => new VcOnEntityDeafenUpdatedPacket());
            var setEntityVisibilityPacket = PacketPool<VcSetEntityVisibilityRequestPacket>
                .GetPacket(() => new VcSetEntityVisibilityRequestPacket());

            //McApi
            var mcApiOnNetworkEntityCreatedPacket = PacketPool<McApiOnNetworkEntityCreatedPacket>
                .GetPacket(() => new McApiOnNetworkEntityCreatedPacket());
            var mcApiOnEntityCreatedPacket = PacketPool<McApiOnEntityCreatedPacket>
                .GetPacket(() => new McApiOnEntityCreatedPacket());

            try
            {
                if (newEntity is VoiceCraftNetworkEntity networkEntity)
                {
                    //Sync up new client first.
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
                            //Create
                            onNetworkEntityCreatedPacket.Set(otherNetworkEntity);
                            SendEvent(networkEntity.NetPeer, onNetworkEntityCreatedPacket);

                            //Server Mute
                            onEntityServerMuteUpdatedPacket.Set(entity.Id, otherNetworkEntity.ServerMuted);
                            SendEvent(networkEntity.NetPeer, onEntityServerMuteUpdatedPacket);

                            //Server Deafen
                            onEntityServerDeafenUpdatedPacket.Set(entity.Id, otherNetworkEntity.ServerDeafened);
                            SendEvent(networkEntity.NetPeer, onEntityServerDeafenUpdatedPacket);
                        }
                        else
                        {
                            //Create
                            onEntityCreatedPacket.Set(entity);
                            SendEvent(networkEntity.NetPeer, onEntityCreatedPacket);
                        }

                        //Name
                        onEntityNameUpdatedPacket.Set(entity.Id, entity.Name);
                        SendEvent(networkEntity.NetPeer, onEntityNameUpdatedPacket);

                        //Mute
                        onEntityMuteUpdatedPacket.Set(entity.Id, entity.Muted);
                        SendEvent(networkEntity.NetPeer, onEntityMuteUpdatedPacket);

                        //Deafen
                        onEntityDeafenUpdatedPacket.Set(entity.Id, entity.Deafened);
                        SendEvent(networkEntity.NetPeer, onEntityDeafenUpdatedPacket);

                        //Set Visibility if disabled.
                        if (EnableVisibilityDisplay) continue;
                        setEntityVisibilityPacket.Set(entity.Id, true);
                        Send(networkEntity.NetPeer, setEntityVisibilityPacket);
                    }

                    //Broadcast to other clients and McApi.
                    onNetworkEntityCreatedPacket.Set(networkEntity);
                    BroadcastEvent(onNetworkEntityCreatedPacket, VcDeliveryMethod.Reliable, networkEntity.NetPeer);
                    if (!EnableVisibilityDisplay)
                    {
                        setEntityVisibilityPacket.Set(networkEntity.Id, true);
                        Broadcast(setEntityVisibilityPacket, VcDeliveryMethod.Reliable, networkEntity.NetPeer);
                    }

                    mcApiOnNetworkEntityCreatedPacket.Set(networkEntity);
                    BroadcastEventMcApi(mcApiOnNetworkEntityCreatedPacket);

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
                onEffectUpdatedPacket.Return();
                onNetworkEntityCreatedPacket.Return();
                onEntityServerMuteUpdatedPacket.Return();
                onEntityServerDeafenUpdatedPacket.Return();
                onEntityCreatedPacket.Return();
                onEntityNameUpdatedPacket.Return();
                onEntityMuteUpdatedPacket.Return();
                onEntityDeafenUpdatedPacket.Return();
                setEntityVisibilityPacket.Return();
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
                BroadcastEvent(vcPacket);

                mcApiPacket.Set(entity.Id);
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
            //Effects
            var onEffectUpdatedPacket = PacketPool<McApiOnEffectUpdatedPacket>
                .GetPacket(() => new McApiOnEffectUpdatedPacket());

            //Network Entity
            var onNetworkEntityCreatedPacket = PacketPool<McApiOnNetworkEntityCreatedPacket>
                .GetPacket(() => new McApiOnNetworkEntityCreatedPacket());
            var onEntityServerMuteUpdatedPacket = PacketPool<McApiOnEntityServerMuteUpdatedPacket>
                .GetPacket(() => new McApiOnEntityServerMuteUpdatedPacket());
            var onEntityServerDeafenUpdatedPacket = PacketPool<McApiOnEntityServerDeafenUpdatedPacket>
                .GetPacket(() => new McApiOnEntityServerDeafenUpdatedPacket());

            //Regular Entity
            var onEntityCreatedPacket = PacketPool<McApiOnEntityCreatedPacket>
                .GetPacket(() => new McApiOnEntityCreatedPacket());
            var onEntityWorldIdUpdatedPacket = PacketPool<McApiOnEntityWorldIdUpdatedPacket>
                .GetPacket(() => new McApiOnEntityWorldIdUpdatedPacket());
            var onEntityNameUpdatedPacket = PacketPool<McApiOnEntityNameUpdatedPacket>
                .GetPacket(() => new McApiOnEntityNameUpdatedPacket());
            var onEntityMuteUpdatedPacket = PacketPool<McApiOnEntityMuteUpdatedPacket>
                .GetPacket(() => new McApiOnEntityMuteUpdatedPacket());
            var onEntityDeafenUpdatedPacket = PacketPool<McApiOnEntityDeafenUpdatedPacket>
                .GetPacket(() => new McApiOnEntityDeafenUpdatedPacket());
            var onEntityPositionUpdatedPacket = PacketPool<McApiOnEntityPositionUpdatedPacket>
                .GetPacket(() => new McApiOnEntityPositionUpdatedPacket());
            var onEntityRotationUpdatedPacket = PacketPool<McApiOnEntityRotationUpdatedPacket>
                .GetPacket(() => new McApiOnEntityRotationUpdatedPacket());
            var onEntityTalkBitmaskUpdatedPacket = PacketPool<McApiOnEntityTalkBitmaskUpdatedPacket>
                .GetPacket(() => new McApiOnEntityTalkBitmaskUpdatedPacket());
            var onEntityListenBitmaskUpdatedPacket = PacketPool<McApiOnEntityListenBitmaskUpdatedPacket>
                .GetPacket(() => new McApiOnEntityListenBitmaskUpdatedPacket());
            var onEntityEffectBitmaskUpdatedPacket = PacketPool<McApiOnEntityEffectBitmaskUpdatedPacket>
                .GetPacket(() => new McApiOnEntityEffectBitmaskUpdatedPacket());
            var onEntityPropertyUpdatedPacket = PacketPool<McApiOnEntityPropertyUpdatedPacket>
                .GetPacket(() => new McApiOnEntityPropertyUpdatedPacket());

            try
            {
                //Sync Effects
                if (peer.SubscribedEvents.Contains(EventType.OnEffectUpdated))
                {
                    var audioEffects = _audioEffectSystem.AudioEffects;
                    foreach (var effect in audioEffects)
                    {
                        onEffectUpdatedPacket.Set(effect.Key, effect.Value);
                        SendEventMcApi(peer, onEffectUpdatedPacket);
                    }
                }

                //Sync Entities
                if (peer.SubscribedEvents.Contains(EventType.OnNetworkEntityCreated) ||
                    peer.SubscribedEvents.Contains(EventType.OnEntityCreated))
                {
                    foreach (var entity in _world.Entities)
                    {
                        if (entity is VoiceCraftNetworkEntity networkEntity)
                        {
                            //Create
                            onNetworkEntityCreatedPacket.Set(networkEntity);
                            SendEventMcApi(peer, onNetworkEntityCreatedPacket);

                            //Server Mute
                            onEntityServerMuteUpdatedPacket.Set(networkEntity.Id, networkEntity.Muted);
                            SendEventMcApi(peer, onEntityServerMuteUpdatedPacket);

                            //Server Deafen
                            onEntityServerDeafenUpdatedPacket.Set(networkEntity.Id, networkEntity.Deafened);
                            SendEventMcApi(peer, onEntityServerDeafenUpdatedPacket);
                        }
                        else
                        {
                            //Create
                            onEntityCreatedPacket.Set(entity);
                            SendEventMcApi(peer, onEntityCreatedPacket);
                        }

                        //WorldId
                        onEntityWorldIdUpdatedPacket.Set(entity.Id, entity.WorldId);
                        SendEventMcApi(peer, onEntityWorldIdUpdatedPacket);

                        //Name
                        onEntityNameUpdatedPacket.Set(entity.Id, entity.Name);
                        SendEventMcApi(peer, onEntityNameUpdatedPacket);

                        //Mute
                        onEntityMuteUpdatedPacket.Set(entity.Id, entity.Muted);
                        SendEventMcApi(peer, onEntityMuteUpdatedPacket);

                        //Deafen
                        onEntityDeafenUpdatedPacket.Set(entity.Id, entity.Deafened);
                        SendEventMcApi(peer, onEntityDeafenUpdatedPacket);

                        //Position
                        onEntityPositionUpdatedPacket.Set(entity.Id, entity.Position);
                        SendEventMcApi(peer, onEntityPositionUpdatedPacket);

                        //Rotation
                        onEntityRotationUpdatedPacket.Set(entity.Id, entity.Rotation);
                        SendEventMcApi(peer, onEntityRotationUpdatedPacket);

                        //TalkBitmask
                        onEntityTalkBitmaskUpdatedPacket.Set(entity.Id, entity.TalkBitmask);
                        SendEventMcApi(peer, onEntityTalkBitmaskUpdatedPacket);

                        //ListenBitmask
                        onEntityListenBitmaskUpdatedPacket.Set(entity.Id, entity.ListenBitmask);
                        SendEventMcApi(peer, onEntityListenBitmaskUpdatedPacket);

                        //Effect Bitmask
                        onEntityEffectBitmaskUpdatedPacket.Set(entity.Id, entity.EffectBitmask);
                        SendEventMcApi(peer, onEntityEffectBitmaskUpdatedPacket);

                        if (!peer.SubscribedEvents.Contains(EventType.OnEntityPropertyUpdated)) continue;
                        //Properties
                        foreach (var property in entity.Properties)
                        {
                            onEntityPropertyUpdatedPacket.Set(entity.Id, property.Key, property.Value);
                            SendEventMcApi(peer, onEntityPropertyUpdatedPacket);
                        }
                    }
                }

                AnsiConsole.MarkupLine($"[green]{Localizer.Get($"Events.McApi.Client.Connected:{token}")}[/]");
            }
            finally
            {
                onEffectUpdatedPacket.Return();

                onNetworkEntityCreatedPacket.Return();
                onEntityServerMuteUpdatedPacket.Return();
                onEntityServerDeafenUpdatedPacket.Return();

                onEntityCreatedPacket.Return();
                onEntityWorldIdUpdatedPacket.Return();
                onEntityNameUpdatedPacket.Return();
                onEntityMuteUpdatedPacket.Return();
                onEntityDeafenUpdatedPacket.Return();
                onEntityPositionUpdatedPacket.Return();
                onEntityRotationUpdatedPacket.Return();
                onEntityTalkBitmaskUpdatedPacket.Return();
                onEntityListenBitmaskUpdatedPacket.Return();
                onEntityEffectBitmaskUpdatedPacket.Return();
                onEntityPropertyUpdatedPacket.Return();
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

    //Network Entity Specifics
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
                Send(entity.NetPeer, setServerMutePacket);

                vcPacket.Set(entity.Id, muted);
                BroadcastEvent(vcPacket, VcDeliveryMethod.Reliable, entity.NetPeer);

                mcApiPacket.Set(entity.Id, muted);
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
                Send(entity.NetPeer, setServerDeafenPacket);

                vcPacket.Set(entity.Id, deafened);
                BroadcastEvent(vcPacket, VcDeliveryMethod.Reliable, entity.NetPeer);

                mcApiPacket.Set(entity.Id, deafened);
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

    //Server Side
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

    //Global
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

                mcApiPacket.Set(entity.Id, name);
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
                if (entity is VoiceCraftNetworkEntity networkEntity)
                    BroadcastEvent(vcPacket, VcDeliveryMethod.Reliable, networkEntity.NetPeer);
                else
                    BroadcastEvent(vcPacket);

                mcApiPacket.Set(entity.Id, mute);
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
                if (entity is VoiceCraftNetworkEntity networkEntity)
                    BroadcastEvent(vcPacket, VcDeliveryMethod.Reliable, networkEntity.NetPeer);
                else
                    BroadcastEvent(vcPacket);

                mcApiPacket.Set(entity.Id, deafen);
                BroadcastEventMcApi(mcApiPacket);
            }
            finally
            {
                vcPacket.Return();
                mcApiPacket.Return();
            }
        });
    }

    //Visible Entities Only
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
                if (entity is VoiceCraftNetworkEntity { PositioningType: PositioningType.Server } networkEntity)
                {
                    setTalkBitmaskPacket.Set(bitmask);
                    Send(networkEntity.NetPeer, setTalkBitmaskPacket);
                }

                vcPacket.Set(entity.Id, bitmask);
                foreach (var ve in entity.VisibleEntities.Cast<VoiceCraftNetworkEntity>())
                {
                    SendEvent(ve.NetPeer, vcPacket);
                }

                mcApiPacket.Set(entity.Id, bitmask);
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
                if (entity is VoiceCraftNetworkEntity { PositioningType: PositioningType.Server } networkEntity)
                {
                    setListenBitmaskPacket.Set(bitmask);
                    Send(networkEntity.NetPeer, setListenBitmaskPacket);
                }

                vcPacket.Set(entity.Id, bitmask);
                foreach (var ve in entity.VisibleEntities.Cast<VoiceCraftNetworkEntity>())
                {
                    SendEvent(ve.NetPeer, vcPacket);
                }

                mcApiPacket.Set(entity.Id, bitmask);
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
                if (entity is VoiceCraftNetworkEntity { PositioningType: PositioningType.Server } networkEntity)
                {
                    setEffectBitmaskPacket.Set(bitmask);
                    Send(networkEntity.NetPeer, setEffectBitmaskPacket);
                }

                vcPacket.Set(entity.Id, bitmask);
                foreach (var ve in entity.VisibleEntities.Cast<VoiceCraftNetworkEntity>())
                {
                    SendEvent(ve.NetPeer, vcPacket);
                }

                mcApiPacket.Set(entity.Id, bitmask);
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

                vcPacket.Set(entity.Id, position);
                foreach (var ve in entity.VisibleEntities.Cast<VoiceCraftNetworkEntity>())
                {
                    SendEvent(ve.NetPeer, vcPacket);
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

                vcPacket.Set(entity.Id, rotation);
                foreach (var ve in entity.VisibleEntities.Cast<VoiceCraftNetworkEntity>())
                {
                    SendEvent(ve.NetPeer, vcPacket);
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
                if (entity is VoiceCraftNetworkEntity { PositioningType: PositioningType.Server } networkEntity)
                {
                    setPropertyPacket.Set(key, value);
                    Send(networkEntity.NetPeer, setPropertyPacket);
                }

                vcPacket.Set(entity.Id, key, value);
                foreach (var ve in entity.VisibleEntities.Cast<VoiceCraftNetworkEntity>())
                {
                    SendEvent(ve.NetPeer, vcPacket);
                }

                mcApiPacket.Set(entity.Id, key, value);
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

    private void OnEntityAudioReceived(byte[] buffer, ushort timestamp, float frameLoudness, VoiceCraftEntity entity)
    {
        _tasks.Enqueue(() =>
        {
            var vcPacket = PacketPool<VcOnEntityAudioDataReceivedPacket>
                .GetPacket(() => new VcOnEntityAudioDataReceivedPacket());
            var mcApiPacket = PacketPool<McApiOnEntityAudioReceivedPacket>
                .GetPacket(() => new McApiOnEntityAudioReceivedPacket());
            var mcApiDataPacket = PacketPool<McApiOnEntityAudioDataReceivedPacket>
                .GetPacket(() => new McApiOnEntityAudioDataReceivedPacket());

            try
            {
                vcPacket.Set(entity.Id, timestamp, frameLoudness, buffer.Length, buffer);
                foreach (var ve in entity.VisibleEntities.Cast<VoiceCraftNetworkEntity>())
                {
                    if (ve == entity || ve.Deafened || ve.ServerDeafened) continue;
                    SendEvent(ve.NetPeer, vcPacket, VcDeliveryMethod.Unreliable);
                }

                mcApiPacket.Set(entity.Id, timestamp, frameLoudness);
                BroadcastEventMcApi(mcApiPacket);

                mcApiDataPacket.Set(entity.Id, timestamp, frameLoudness, buffer.Length, buffer);
                BroadcastEventMcApi(mcApiDataPacket);
            }
            finally
            {
                vcPacket.Return();
                mcApiPacket.Return();
                mcApiDataPacket.Return();
            }
        });
    }

    //Visible Entities
    private void OnEntityVisibleEntityAdded(VoiceCraftEntity addedEntity, VoiceCraftEntity entity)
    {
        _tasks.Enqueue(() =>
        {
            var visibilityPacket = PacketPool<VcSetEntityVisibilityRequestPacket>
                .GetPacket(() => new VcSetEntityVisibilityRequestPacket());
            var onEntityPositionUpdatedPacket = PacketPool<VcOnEntityPositionUpdatedPacket>
                .GetPacket(() => new VcOnEntityPositionUpdatedPacket());
            var onEntityRotationUpdatedPacket = PacketPool<VcOnEntityRotationUpdatedPacket>
                .GetPacket(() => new VcOnEntityRotationUpdatedPacket());
            var onEntityTalkBitmaskUpdatedPacket = PacketPool<VcOnEntityTalkBitmaskUpdatedPacket>
                .GetPacket(() => new VcOnEntityTalkBitmaskUpdatedPacket());
            var onEntityListenBitmaskUpdatedPacket = PacketPool<VcOnEntityListenBitmaskUpdatedPacket>
                .GetPacket(() => new VcOnEntityListenBitmaskUpdatedPacket());
            var onEntityEffectBitmaskUpdatedPacket = PacketPool<VcOnEntityEffectBitmaskUpdatedPacket>
                .GetPacket(() => new VcOnEntityEffectBitmaskUpdatedPacket());
            var onEntityPropertyUpdatedPacket = PacketPool<VcOnEntityPropertyUpdatedPacket>
                .GetPacket(() => new VcOnEntityPropertyUpdatedPacket());
            var onEntityVisibilityUpdatedPacket = PacketPool<McApiOnEntityVisibilityUpdatedPacket>
                .GetPacket(() => new McApiOnEntityVisibilityUpdatedPacket());

            try
            {
                if (addedEntity is VoiceCraftNetworkEntity networkEntity)
                {
                    if (EnableVisibilityDisplay)
                    {
                        visibilityPacket.Set(entity.Id, true);
                        Send(networkEntity.NetPeer, visibilityPacket);
                    }

                    //Position
                    onEntityPositionUpdatedPacket.Set(entity.Id, entity.Position);
                    SendEvent(networkEntity.NetPeer, onEntityPositionUpdatedPacket);

                    //Rotation
                    onEntityRotationUpdatedPacket.Set(entity.Id, entity.Rotation);
                    SendEvent(networkEntity.NetPeer, onEntityRotationUpdatedPacket);

                    //Talk
                    onEntityTalkBitmaskUpdatedPacket.Set(entity.Id, entity.TalkBitmask);
                    SendEvent(networkEntity.NetPeer, onEntityTalkBitmaskUpdatedPacket);

                    //Listen
                    onEntityListenBitmaskUpdatedPacket.Set(entity.Id, entity.ListenBitmask);
                    SendEvent(networkEntity.NetPeer, onEntityListenBitmaskUpdatedPacket);

                    //Effect
                    onEntityEffectBitmaskUpdatedPacket.Set(entity.Id, entity.EffectBitmask);
                    SendEvent(networkEntity.NetPeer, onEntityEffectBitmaskUpdatedPacket);

                    //Properties
                    foreach (var property in entity.Properties)
                    {
                        onEntityPropertyUpdatedPacket.Set(entity.Id, property.Key, property.Value);
                        SendEvent(networkEntity.NetPeer, onEntityPropertyUpdatedPacket);
                    }
                }

                onEntityVisibilityUpdatedPacket.Set(entity.Id, addedEntity.Id, true);
                BroadcastEventMcApi(onEntityVisibilityUpdatedPacket);
            }
            finally
            {
                visibilityPacket.Return();
                onEntityPositionUpdatedPacket.Return();
                onEntityRotationUpdatedPacket.Return();
                onEntityTalkBitmaskUpdatedPacket.Return();
                onEntityListenBitmaskUpdatedPacket.Return();
                onEntityEffectBitmaskUpdatedPacket.Return();
                onEntityPropertyUpdatedPacket.Return();
                onEntityVisibilityUpdatedPacket.Return();
            }
        });
    }

    private void OnEntityVisibleEntityRemoved(VoiceCraftEntity removedEntity, VoiceCraftEntity entity)
    {
        _tasks.Enqueue(() =>
        {
            var visibilityPacket = PacketPool<VcSetEntityVisibilityRequestPacket>
                .GetPacket(() => new VcSetEntityVisibilityRequestPacket());
            var onEntityPropertyUpdatedPacket = PacketPool<VcOnEntityPropertyUpdatedPacket>
                .GetPacket(() => new VcOnEntityPropertyUpdatedPacket());
            var onEntityVisibilityUpdatedPacket = PacketPool<McApiOnEntityVisibilityUpdatedPacket>
                .GetPacket(() => new McApiOnEntityVisibilityUpdatedPacket());

            try
            {
                if (removedEntity is VoiceCraftNetworkEntity networkEntity)
                {
                    //Update visibility if enabled.
                    if (EnableVisibilityDisplay)
                    {
                        visibilityPacket.Set(entity.Id);
                        Send(networkEntity.NetPeer, visibilityPacket);
                    }

                    //Clear Properties
                    foreach (var property in entity.Properties)
                    {
                        onEntityPropertyUpdatedPacket.Set(entity.Id, property.Key);
                        SendEvent(networkEntity.NetPeer, onEntityPropertyUpdatedPacket);
                    }
                }

                onEntityVisibilityUpdatedPacket.Set(entity.Id, removedEntity.Id);
                BroadcastEventMcApi(onEntityVisibilityUpdatedPacket);
            }
            finally
            {
                visibilityPacket.Return();
                onEntityPropertyUpdatedPacket.Return();
                onEntityVisibilityUpdatedPacket.Return();
            }
        });
    }

    #endregion
}