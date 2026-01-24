using System.Collections.Concurrent;
using System.Numerics;
using Spectre.Console;
using VoiceCraft.Core.Locales;
using VoiceCraft.Core.World;
using VoiceCraft.Network;
using VoiceCraft.Network.Interfaces;
using VoiceCraft.Network.NetPeers;
using VoiceCraft.Network.Packets.McApiPackets.Event;
using VoiceCraft.Network.Packets.VcPackets.Event;
using VoiceCraft.Network.Packets.VcPackets.Request;
using VoiceCraft.Network.Servers;
using VoiceCraft.Network.Systems;
using VoiceCraft.Network.World;

namespace VoiceCraft.Server.Systems;

public class EventHandlerSystem : IDisposable
{
    private readonly AudioEffectSystem _audioEffectSystem;
    private readonly HttpMcApiServer _httpMcApiServer;
    private readonly McWssMcApiServer _mcWssMcApiServer;
    private readonly LiteNetVoiceCraftServer _liteNetServer;
    private readonly ConcurrentQueue<Action> _tasks = [];
    private readonly VoiceCraftWorld _world;
    public bool EnableVisibilityDisplay { get; set; }

    public EventHandlerSystem(
        LiteNetVoiceCraftServer liteNetServer,
        HttpMcApiServer httpMcApiServer,
        McWssMcApiServer mcWssMcApiServer,
        AudioEffectSystem audioEffectSystem,
        VoiceCraftWorld world)
    {
        _liteNetServer = liteNetServer;
        _httpMcApiServer = httpMcApiServer;
        _mcWssMcApiServer = mcWssMcApiServer;
        _audioEffectSystem = audioEffectSystem;
        _world = world;

        _world.OnEntityCreated += OnEntityCreated;
        _world.OnEntityDestroyed += OnEntityDestroyed;
        _audioEffectSystem.OnEffectSet += OnAudioEffectSet;
        _httpMcApiServer.OnPeerConnected += OnMcHttpMcApiPeerConnected;
        _httpMcApiServer.OnPeerDisconnected += OnMcHttpMcApiPeerDisconnected;
        _mcWssMcApiServer.OnPeerConnected += OnMcWssMcApiPeerConnected;
        _mcWssMcApiServer.OnPeerDisconnected += OnMcWssMcApiPeerDisconnected;
    }

    public void Dispose()
    {
        _world.OnEntityCreated -= OnEntityCreated;
        _world.OnEntityDestroyed -= OnEntityDestroyed;
        _audioEffectSystem.OnEffectSet -= OnAudioEffectSet;
        _httpMcApiServer.OnPeerConnected -= OnMcHttpMcApiPeerConnected;
        _httpMcApiServer.OnPeerDisconnected -= OnMcHttpMcApiPeerDisconnected;
        _mcWssMcApiServer.OnPeerConnected -= OnMcWssMcApiPeerConnected;
        _mcWssMcApiServer.OnPeerDisconnected -= OnMcWssMcApiPeerDisconnected;
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

    #region Audio Effect Events

    private void OnAudioEffectSet(ushort bitmask, IAudioEffect? effect)
    {
        _tasks.Enqueue(() =>
        {
            _liteNetServer.Broadcast(PacketPool<VcOnEffectUpdatedPacket>.GetPacket(() => new VcOnEffectUpdatedPacket())
                .Set(bitmask, effect));
            _httpMcApiServer.Broadcast(PacketPool<McApiOnEffectUpdatedPacket>
                .GetPacket(() => new McApiOnEffectUpdatedPacket()).Set(bitmask, effect));
            _mcWssMcApiServer.Broadcast(PacketPool<McApiOnEffectUpdatedPacket>
                .GetPacket(() => new McApiOnEffectUpdatedPacket()).Set(bitmask, effect));
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
                _liteNetServer.SendPacket(networkEntity.NetPeer,
                    PacketPool<VcSetNameRequestPacket>.GetPacket(() => new VcSetNameRequestPacket())
                        .Set(networkEntity.Name));
                _liteNetServer.SendPacket(networkEntity.NetPeer,
                    PacketPool<VcSetServerMuteRequestPacket>.GetPacket(() => new VcSetServerMuteRequestPacket())
                        .Set(networkEntity.ServerMuted));
                _liteNetServer.SendPacket(networkEntity.NetPeer,
                    PacketPool<VcSetServerDeafenRequestPacket>.GetPacket(() => new VcSetServerDeafenRequestPacket())
                        .Set(networkEntity.ServerDeafened));
                _liteNetServer.Broadcast(
                    PacketPool<VcOnNetworkEntityCreatedPacket>.GetPacket(() => new VcOnNetworkEntityCreatedPacket())
                        .Set(networkEntity),
                    VcDeliveryMethod.Reliable, networkEntity.NetPeer);
                if (!EnableVisibilityDisplay)
                {
                    _liteNetServer.Broadcast(
                        PacketPool<VcSetEntityVisibilityRequestPacket>
                            .GetPacket(() => new VcSetEntityVisibilityRequestPacket()).Set(networkEntity.Id, true),
                        VcDeliveryMethod.Reliable, networkEntity.NetPeer);
                }

                _httpMcApiServer.Broadcast(PacketPool<McApiOnNetworkEntityCreatedPacket>
                    .GetPacket(() => new McApiOnNetworkEntityCreatedPacket()).Set(networkEntity));
                _mcWssMcApiServer.Broadcast(PacketPool<McApiOnNetworkEntityCreatedPacket>
                    .GetPacket(() => new McApiOnNetworkEntityCreatedPacket()).Set(networkEntity));

                //Send Effects
                foreach (var effect in _audioEffectSystem.AudioEffects)
                    _liteNetServer.SendPacket(networkEntity.NetPeer,
                        PacketPool<VcOnEffectUpdatedPacket>.GetPacket(() => new VcOnEffectUpdatedPacket())
                            .Set(effect.Key, effect.Value));

                //Send other entities.
                foreach (var entity in _world.Entities.Where(x => x != networkEntity))
                {
                    if (entity is VoiceCraftNetworkEntity otherNetworkEntity)
                        _liteNetServer.SendPacket(networkEntity.NetPeer,
                            PacketPool<VcOnNetworkEntityCreatedPacket>
                                .GetPacket(() => new VcOnNetworkEntityCreatedPacket()).Set(otherNetworkEntity));
                    else
                        _liteNetServer.SendPacket(networkEntity.NetPeer,
                            PacketPool<VcOnEntityCreatedPacket>.GetPacket(() => new VcOnEntityCreatedPacket())
                                .Set(entity));

                    if (EnableVisibilityDisplay) continue;
                    _liteNetServer.SendPacket(networkEntity.NetPeer, PacketPool<VcSetEntityVisibilityRequestPacket>
                        .GetPacket(() => new VcSetEntityVisibilityRequestPacket())
                        .Set(entity.Id, true));
                }

                AnsiConsole.MarkupLine(
                    $"[green]{Localizer.Get($"Events.Client.Connected:{networkEntity.UserGuid}")}[/]");
            }
            else
            {
                _liteNetServer.Broadcast(PacketPool<VcOnEntityCreatedPacket>
                    .GetPacket(() => new VcOnEntityCreatedPacket()).Set(newEntity));
                _httpMcApiServer.Broadcast(PacketPool<McApiOnEntityCreatedPacket>
                    .GetPacket(() => new McApiOnEntityCreatedPacket()).Set(newEntity));
                _mcWssMcApiServer.Broadcast(PacketPool<McApiOnEntityCreatedPacket>
                    .GetPacket(() => new McApiOnEntityCreatedPacket()).Set(newEntity));
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
            var entityDestroyedPacket = PacketPool<VcOnEntityDestroyedPacket>
                .GetPacket(() => new VcOnEntityDestroyedPacket()).Set(entity.Id);
            if (entity is VoiceCraftNetworkEntity networkEntity)
            {
                _liteNetServer.Disconnect(networkEntity.NetPeer, "VoiceCraft.DisconnectReason.Kicked");
                AnsiConsole.MarkupLine(
                    $"[yellow]{Localizer.Get($"Events.Client.Disconnected:{networkEntity.UserGuid}")}[/]");
            }

            _liteNetServer.Broadcast(entityDestroyedPacket);
            _httpMcApiServer.Broadcast(PacketPool<McApiOnEntityDestroyedPacket>
                .GetPacket(() => new McApiOnEntityDestroyedPacket()).Set(entity.Id));
            _mcWssMcApiServer.Broadcast(PacketPool<McApiOnEntityDestroyedPacket>
                .GetPacket(() => new McApiOnEntityDestroyedPacket()).Set(entity.Id));
        });
    }

    private void OnMcHttpMcApiPeerConnected(McApiNetPeer peer, string token)
    {
        _tasks.Enqueue(() =>
        {
            //Send Effects
            foreach (var effect in _audioEffectSystem.AudioEffects)
                _httpMcApiServer.SendPacket(peer,
                    PacketPool<McApiOnEffectUpdatedPacket>.GetPacket(() => new McApiOnEffectUpdatedPacket())
                        .Set(effect.Key, effect.Value));

            //Send other entities.
            foreach (var entity in _world.Entities)
                if (entity is VoiceCraftNetworkEntity otherNetworkEntity)
                    _httpMcApiServer.SendPacket(peer,
                        PacketPool<McApiOnNetworkEntityCreatedPacket>
                            .GetPacket(() => new McApiOnNetworkEntityCreatedPacket()).Set(otherNetworkEntity));
                else
                    _httpMcApiServer.SendPacket(peer,
                        PacketPool<McApiOnEntityCreatedPacket>.GetPacket(() => new McApiOnEntityCreatedPacket())
                            .Set(entity));

            AnsiConsole.MarkupLine($"[green]{Localizer.Get($"Events.McApi.Client.Connected:{token}")}[/]");
        });
    }

    private void OnMcHttpMcApiPeerDisconnected(McApiNetPeer peer, string token)
    {
        _tasks.Enqueue(() =>
        {
            AnsiConsole.MarkupLine($"[yellow]{Localizer.Get($"Events.McApi.Client.Disconnected:{token}")}[/]");
        });
    }

    private void OnMcWssMcApiPeerConnected(McApiNetPeer peer, string token)
    {
        _tasks.Enqueue(() =>
        {
            //Send Effects
            foreach (var effect in _audioEffectSystem.AudioEffects)
                _mcWssMcApiServer.SendPacket(peer,
                    PacketPool<McApiOnEffectUpdatedPacket>.GetPacket(() => new McApiOnEffectUpdatedPacket())
                        .Set(effect.Key, effect.Value));

            //Send other entities.
            foreach (var entity in _world.Entities)
                if (entity is VoiceCraftNetworkEntity otherNetworkEntity)
                    _mcWssMcApiServer.SendPacket(peer,
                        PacketPool<McApiOnNetworkEntityCreatedPacket>
                            .GetPacket(() => new McApiOnNetworkEntityCreatedPacket()).Set(otherNetworkEntity));
                else
                    _mcWssMcApiServer.SendPacket(peer,
                        PacketPool<McApiOnEntityCreatedPacket>.GetPacket(() => new McApiOnEntityCreatedPacket())
                            .Set(entity));

            AnsiConsole.MarkupLine($"[green]{Localizer.Get($"Events.McApi.Client.Connected:{token}")}[/]");
        });
    }

    private void OnMcWssMcApiPeerDisconnected(McApiNetPeer peer, string token)
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
            _liteNetServer.SendPacket(entity.NetPeer,
                PacketPool<VcSetTitleRequestPacket>.GetPacket(() => new VcSetTitleRequestPacket()).Set(title));
        });
    }

    private void OnNetworkEntitySetDescription(string description, VoiceCraftNetworkEntity entity)
    {
        _tasks.Enqueue(() =>
        {
            _liteNetServer.SendPacket(entity.NetPeer,
                PacketPool<VcSetDescriptionRequestPacket>.GetPacket(() => new VcSetDescriptionRequestPacket())
                    .Set(description));
        });
    }

    private void OnNetworkEntityServerMuteUpdated(bool muted, VoiceCraftNetworkEntity entity)
    {
        _tasks.Enqueue(() =>
        {
            _liteNetServer.SendPacket(entity.NetPeer,
                PacketPool<VcSetServerMuteRequestPacket>.GetPacket(() => new VcSetServerMuteRequestPacket())
                    .Set(muted));
            _liteNetServer.Broadcast(
                PacketPool<VcOnEntityServerMuteUpdatedPacket>.GetPacket(() => new VcOnEntityServerMuteUpdatedPacket())
                    .Set(entity.Id, muted),
                VcDeliveryMethod.Reliable, entity.NetPeer);
            _httpMcApiServer.Broadcast(PacketPool<McApiOnEntityServerMuteUpdatedPacket>
                .GetPacket(() => new McApiOnEntityServerMuteUpdatedPacket())
                .Set(entity.Id, muted));
            _mcWssMcApiServer.Broadcast(PacketPool<McApiOnEntityServerMuteUpdatedPacket>
                .GetPacket(() => new McApiOnEntityServerMuteUpdatedPacket())
                .Set(entity.Id, muted));
        });
    }

    private void OnNetworkEntityServerDeafenUpdated(bool deafened, VoiceCraftNetworkEntity entity)
    {
        _tasks.Enqueue(() =>
        {
            _liteNetServer.SendPacket(entity.NetPeer,
                PacketPool<VcSetServerDeafenRequestPacket>.GetPacket(() => new VcSetServerDeafenRequestPacket())
                    .Set(deafened));
            _liteNetServer.Broadcast(
                PacketPool<VcOnEntityServerDeafenUpdatedPacket>
                    .GetPacket(() => new VcOnEntityServerDeafenUpdatedPacket()).Set(entity.Id, deafened),
                VcDeliveryMethod.Reliable, entity.NetPeer);
            _httpMcApiServer.Broadcast(PacketPool<McApiOnEntityServerDeafenUpdatedPacket>
                .GetPacket(() => new McApiOnEntityServerDeafenUpdatedPacket())
                .Set(entity.Id, deafened));
            _mcWssMcApiServer.Broadcast(PacketPool<McApiOnEntityServerDeafenUpdatedPacket>
                .GetPacket(() => new McApiOnEntityServerDeafenUpdatedPacket())
                .Set(entity.Id, deafened));
        });
    }

    private void OnEntityWorldIdUpdated(string worldId, VoiceCraftEntity entity)
    {
        _tasks.Enqueue(() =>
        {
            _httpMcApiServer.Broadcast(PacketPool<McApiOnEntityWorldIdUpdatedPacket>
                .GetPacket(() => new McApiOnEntityWorldIdUpdatedPacket())
                .Set(entity.Id, worldId));
            _mcWssMcApiServer.Broadcast(PacketPool<McApiOnEntityWorldIdUpdatedPacket>
                .GetPacket(() => new McApiOnEntityWorldIdUpdatedPacket())
                .Set(entity.Id, worldId));
        });
    }

    private void OnEntityNameUpdated(string name, VoiceCraftEntity entity)
    {
        _tasks.Enqueue(() =>
        {
            var packet = PacketPool<VcOnEntityNameUpdatedPacket>.GetPacket(() => new VcOnEntityNameUpdatedPacket())
                .Set(entity.Id, name);
            if (entity is VoiceCraftNetworkEntity networkEntity)
            {
                if (networkEntity.PositioningType == PositioningType.Server)
                    _liteNetServer.SendPacket(networkEntity.NetPeer,
                        PacketPool<VcSetNameRequestPacket>.GetPacket(() => new VcSetNameRequestPacket()).Set(name));
            }
            else
            {
                _liteNetServer.Broadcast(packet);
            }

            _httpMcApiServer.Broadcast(PacketPool<McApiOnEntityNameUpdatedPacket>
                .GetPacket(() => new McApiOnEntityNameUpdatedPacket()).Set(entity.Id, name));
            _mcWssMcApiServer.Broadcast(PacketPool<McApiOnEntityNameUpdatedPacket>
                .GetPacket(() => new McApiOnEntityNameUpdatedPacket()).Set(entity.Id, name));
        });
    }

    private void OnEntityMuteUpdated(bool mute, VoiceCraftEntity entity)
    {
        _tasks.Enqueue(() =>
        {
            var packet = PacketPool<VcOnEntityMuteUpdatedPacket>.GetPacket(() => new VcOnEntityMuteUpdatedPacket())
                .Set(entity.Id, mute);
            if (entity is VoiceCraftNetworkEntity networkEntity)
                _liteNetServer.Broadcast(packet, VcDeliveryMethod.Reliable, networkEntity.NetPeer);
            else
                _liteNetServer.Broadcast(packet);

            _httpMcApiServer.Broadcast(PacketPool<McApiOnEntityMuteUpdatedPacket>
                .GetPacket(() => new McApiOnEntityMuteUpdatedPacket()).Set(entity.Id, mute));
            _mcWssMcApiServer.Broadcast(PacketPool<McApiOnEntityMuteUpdatedPacket>
                .GetPacket(() => new McApiOnEntityMuteUpdatedPacket()).Set(entity.Id, mute));
        });
    }

    private void OnEntityDeafenUpdated(bool deafen, VoiceCraftEntity entity)
    {
        _tasks.Enqueue(() =>
        {
            var packet = PacketPool<VcOnEntityDeafenUpdatedPacket>.GetPacket(() => new VcOnEntityDeafenUpdatedPacket())
                .Set(entity.Id, deafen);
            if (entity is VoiceCraftNetworkEntity networkEntity)
                _liteNetServer.Broadcast(packet, VcDeliveryMethod.Reliable, networkEntity.NetPeer);
            else
                _liteNetServer.Broadcast(packet);

            _httpMcApiServer.Broadcast(PacketPool<McApiOnEntityDeafenUpdatedPacket>
                .GetPacket(() => new McApiOnEntityDeafenUpdatedPacket()).Set(entity.Id, deafen));
            _mcWssMcApiServer.Broadcast(PacketPool<McApiOnEntityDeafenUpdatedPacket>
                .GetPacket(() => new McApiOnEntityDeafenUpdatedPacket()).Set(entity.Id, deafen));
        });
    }

    private void OnEntityTalkBitmaskUpdated(ushort bitmask, VoiceCraftEntity entity)
    {
        _tasks.Enqueue(() =>
        {
            var packet = PacketPool<VcOnEntityTalkBitmaskUpdatedPacket>
                .GetPacket(() => new VcOnEntityTalkBitmaskUpdatedPacket()).Set(entity.Id, bitmask);
            if (entity is VoiceCraftNetworkEntity networkEntity)
            {
                _liteNetServer.SendPacket(networkEntity.NetPeer,
                    PacketPool<VcSetTalkBitmaskRequestPacket>.GetPacket(() => new VcSetTalkBitmaskRequestPacket())
                        .Set(bitmask));
                _liteNetServer.Broadcast(packet, VcDeliveryMethod.Reliable, networkEntity.NetPeer);
            }
            else
            {
                _liteNetServer.Broadcast(packet);
            }

            _httpMcApiServer.Broadcast(PacketPool<McApiOnEntityTalkBitmaskUpdatedPacket>
                .GetPacket(() => new McApiOnEntityTalkBitmaskUpdatedPacket())
                .Set(entity.Id, bitmask));
            _mcWssMcApiServer.Broadcast(PacketPool<McApiOnEntityTalkBitmaskUpdatedPacket>
                .GetPacket(() => new McApiOnEntityTalkBitmaskUpdatedPacket())
                .Set(entity.Id, bitmask));
        });
    }

    private void OnEntityListenBitmaskUpdated(ushort bitmask, VoiceCraftEntity entity)
    {
        _tasks.Enqueue(() =>
        {
            var packet = PacketPool<VcOnEntityListenBitmaskUpdatedPacket>
                .GetPacket(() => new VcOnEntityListenBitmaskUpdatedPacket()).Set(entity.Id, bitmask);
            if (entity is VoiceCraftNetworkEntity networkEntity)
            {
                _liteNetServer.SendPacket(networkEntity.NetPeer,
                    PacketPool<VcSetListenBitmaskRequestPacket>.GetPacket(() => new VcSetListenBitmaskRequestPacket())
                        .Set(bitmask));
                _liteNetServer.Broadcast(packet, VcDeliveryMethod.Reliable, networkEntity.NetPeer);
            }
            else
            {
                _liteNetServer.Broadcast(packet);
            }

            _httpMcApiServer.Broadcast(PacketPool<McApiOnEntityListenBitmaskUpdatedPacket>
                .GetPacket(() => new McApiOnEntityListenBitmaskUpdatedPacket())
                .Set(entity.Id, bitmask));
            _mcWssMcApiServer.Broadcast(PacketPool<McApiOnEntityListenBitmaskUpdatedPacket>
                .GetPacket(() => new McApiOnEntityListenBitmaskUpdatedPacket())
                .Set(entity.Id, bitmask));
        });
    }

    private void OnEntityEffectBitmaskUpdated(ushort bitmask, VoiceCraftEntity entity)
    {
        _tasks.Enqueue(() =>
        {
            var packet = PacketPool<VcOnEntityEffectBitmaskUpdatedPacket>
                .GetPacket(() => new VcOnEntityEffectBitmaskUpdatedPacket()).Set(entity.Id, bitmask);
            if (entity is VoiceCraftNetworkEntity networkEntity)
            {
                _liteNetServer.SendPacket(networkEntity.NetPeer,
                    PacketPool<VcSetEffectBitmaskRequestPacket>.GetPacket(() => new VcSetEffectBitmaskRequestPacket())
                        .Set(bitmask));
                _liteNetServer.Broadcast(packet, VcDeliveryMethod.Reliable, networkEntity.NetPeer);
            }
            else
            {
                _liteNetServer.Broadcast(packet);
            }

            _httpMcApiServer.Broadcast(PacketPool<McApiOnEntityEffectBitmaskUpdatedPacket>
                .GetPacket(() => new McApiOnEntityEffectBitmaskUpdatedPacket())
                .Set(entity.Id, bitmask));
            _mcWssMcApiServer.Broadcast(PacketPool<McApiOnEntityEffectBitmaskUpdatedPacket>
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
                _liteNetServer.SendPacket(networkEntity.NetPeer,
                    PacketPool<VcSetPositionRequestPacket>.GetPacket(() => new VcSetPositionRequestPacket())
                        .Set(position));

            foreach (var ve in entity.VisibleEntities)
            {
                if (ve is not VoiceCraftNetworkEntity visibleEntity) continue;
                var packet = PacketPool<VcOnEntityPositionUpdatedPacket>
                    .GetPacket(() => new VcOnEntityPositionUpdatedPacket()).Set(entity.Id, position);
                _liteNetServer.SendPacket(visibleEntity.NetPeer, packet);
            }

            _httpMcApiServer.Broadcast(PacketPool<McApiOnEntityPositionUpdatedPacket>
                .GetPacket(() => new McApiOnEntityPositionUpdatedPacket())
                .Set(entity.Id, position));
            _mcWssMcApiServer.Broadcast(PacketPool<McApiOnEntityPositionUpdatedPacket>
                .GetPacket(() => new McApiOnEntityPositionUpdatedPacket())
                .Set(entity.Id, position));
        });
    }

    private void OnEntityRotationUpdated(Vector2 rotation, VoiceCraftEntity entity)
    {
        _tasks.Enqueue(() =>
        {
            if (entity is VoiceCraftNetworkEntity { PositioningType: PositioningType.Server } networkEntity)
                _liteNetServer.SendPacket(networkEntity.NetPeer,
                    PacketPool<VcSetRotationRequestPacket>.GetPacket(() => new VcSetRotationRequestPacket())
                        .Set(rotation));

            foreach (var ve in entity.VisibleEntities)
            {
                if (ve is not VoiceCraftNetworkEntity visibleEntity) continue;
                var packet = PacketPool<VcOnEntityRotationUpdatedPacket>
                    .GetPacket(() => new VcOnEntityRotationUpdatedPacket()).Set(entity.Id, rotation);
                _liteNetServer.SendPacket(visibleEntity.NetPeer, packet);
            }

            _httpMcApiServer.Broadcast(PacketPool<McApiOnEntityRotationUpdatedPacket>
                .GetPacket(() => new McApiOnEntityRotationUpdatedPacket())
                .Set(entity.Id, rotation));
            _mcWssMcApiServer.Broadcast(PacketPool<McApiOnEntityRotationUpdatedPacket>
                .GetPacket(() => new McApiOnEntityRotationUpdatedPacket())
                .Set(entity.Id, rotation));
        });
    }

    private void OnEntityCaveFactorUpdated(float caveFactor, VoiceCraftEntity entity)
    {
        _tasks.Enqueue(() =>
        {
            if (entity is VoiceCraftNetworkEntity { PositioningType: PositioningType.Server } networkEntity)
                _liteNetServer.SendPacket(networkEntity.NetPeer,
                    PacketPool<VcSetCaveFactorRequest>.GetPacket(() => new VcSetCaveFactorRequest()).Set(caveFactor));

            foreach (var ve in entity.VisibleEntities)
            {
                if (ve is not VoiceCraftNetworkEntity visibleEntity) continue;
                var packet = PacketPool<VcOnEntityCaveFactorUpdatedPacket>
                    .GetPacket(() => new VcOnEntityCaveFactorUpdatedPacket()).Set(entity.Id, caveFactor);
                _liteNetServer.SendPacket(visibleEntity.NetPeer, packet);
            }

            _httpMcApiServer.Broadcast(PacketPool<McApiOnEntityCaveFactorUpdatedPacket>
                .GetPacket(() => new McApiOnEntityCaveFactorUpdatedPacket())
                .Set(entity.Id, caveFactor));
            _mcWssMcApiServer.Broadcast(PacketPool<McApiOnEntityCaveFactorUpdatedPacket>
                .GetPacket(() => new McApiOnEntityCaveFactorUpdatedPacket())
                .Set(entity.Id, caveFactor));
        });
    }

    private void OnEntityMuffleFactorUpdated(float muffleFactor, VoiceCraftEntity entity)
    {
        _tasks.Enqueue(() =>
        {
            if (entity is VoiceCraftNetworkEntity { PositioningType: PositioningType.Server } networkEntity)
                _liteNetServer.SendPacket(networkEntity.NetPeer,
                    PacketPool<VcSetMuffleFactorRequest>.GetPacket(() => new VcSetMuffleFactorRequest())
                        .Set(muffleFactor));

            foreach (var ve in entity.VisibleEntities)
            {
                if (ve is not VoiceCraftNetworkEntity visibleEntity) continue;
                var packet = PacketPool<VcOnEntityMuffleFactorUpdatedPacket>
                    .GetPacket(() => new VcOnEntityMuffleFactorUpdatedPacket()).Set(entity.Id, muffleFactor);
                _liteNetServer.SendPacket(visibleEntity.NetPeer, packet);
            }

            _httpMcApiServer.Broadcast(PacketPool<McApiOnEntityMuffleFactorUpdatedPacket>
                .GetPacket(() => new McApiOnEntityMuffleFactorUpdatedPacket())
                .Set(entity.Id, muffleFactor));
            _mcWssMcApiServer.Broadcast(PacketPool<McApiOnEntityMuffleFactorUpdatedPacket>
                .GetPacket(() => new McApiOnEntityMuffleFactorUpdatedPacket())
                .Set(entity.Id, muffleFactor));
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
                    _liteNetServer.SendPacket(networkEntity.NetPeer, visibilityPacket);
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

                _liteNetServer.SendPacket(networkEntity.NetPeer, positionPacket);
                _liteNetServer.SendPacket(networkEntity.NetPeer, rotationPacket);
                _liteNetServer.SendPacket(networkEntity.NetPeer, caveFactorPacket);
                _liteNetServer.SendPacket(networkEntity.NetPeer, muffleFactorPacket);
            }

            _httpMcApiServer.Broadcast(PacketPool<McApiOnEntityVisibilityUpdatedPacket>
                .GetPacket(() => new McApiOnEntityVisibilityUpdatedPacket())
                .Set(entity.Id, addedEntity.Id, true));
            _mcWssMcApiServer.Broadcast(PacketPool<McApiOnEntityVisibilityUpdatedPacket>
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
                    _liteNetServer.SendPacket(networkEntity.NetPeer,
                        PacketPool<VcSetEntityVisibilityRequestPacket>
                            .GetPacket(() => new VcSetEntityVisibilityRequestPacket()).Set(entity.Id));
                }
            }

            _httpMcApiServer.Broadcast(PacketPool<McApiOnEntityVisibilityUpdatedPacket>
                .GetPacket(() => new McApiOnEntityVisibilityUpdatedPacket())
                .Set(entity.Id, removedEntity.Id));
            _mcWssMcApiServer.Broadcast(PacketPool<McApiOnEntityVisibilityUpdatedPacket>
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
                var packet = PacketPool<VcOnEntityAudioReceivedPacket>
                    .GetPacket(() => new VcOnEntityAudioReceivedPacket())
                    .Set(entity.Id, timestamp, frameLoudness, buffer.Length, buffer);
                _liteNetServer.SendPacket(visibleEntity.NetPeer, packet, VcDeliveryMethod.Unreliable);
            }

            _httpMcApiServer.Broadcast(PacketPool<McApiOnEntityAudioReceivedPacket>
                .GetPacket(() => new McApiOnEntityAudioReceivedPacket())
                .Set(entity.Id, timestamp, frameLoudness));
            _mcWssMcApiServer.Broadcast(PacketPool<McApiOnEntityAudioReceivedPacket>
                .GetPacket(() => new McApiOnEntityAudioReceivedPacket())
                .Set(entity.Id, timestamp, frameLoudness));
        });
    }

    #endregion
}