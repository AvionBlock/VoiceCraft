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
    private readonly LiteNetVoiceCraftServer _liteNetServer;
    private readonly ConcurrentQueue<Action> _tasks = [];
    private readonly VoiceCraftWorld _world;
    public bool EnableVisibilityDisplay { get; set; }

    public EventHandlerSystem(
        LiteNetVoiceCraftServer liteNetServer,
        HttpMcApiServer httpMcApiServer,
        AudioEffectSystem audioEffectSystem,
        VoiceCraftWorld world)
    {
        _liteNetServer = liteNetServer;
        _httpMcApiServer = httpMcApiServer;
        _audioEffectSystem = audioEffectSystem;
        _world = world;

        _world.OnEntityCreated += OnEntityCreated;
        _world.OnEntityDestroyed += OnEntityDestroyed;
        _audioEffectSystem.OnEffectSet += OnAudioEffectSet;
        _httpMcApiServer.OnPeerConnected += OnMcHttpMcApiPeerConnected;
        _httpMcApiServer.OnPeerDisconnected += OnMcHttpMcApiPeerDisconnected;
    }

    public void Dispose()
    {
        _world.OnEntityCreated -= OnEntityCreated;
        _world.OnEntityDestroyed -= OnEntityDestroyed;
        _audioEffectSystem.OnEffectSet -= OnAudioEffectSet;
        _httpMcApiServer.OnPeerConnected -= OnMcHttpMcApiPeerConnected;
        _httpMcApiServer.OnPeerDisconnected -= OnMcHttpMcApiPeerDisconnected;
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
            _liteNetServer.Broadcast(PacketPool<VcOnEffectUpdatedPacket>.GetPacket().Set(bitmask, effect));
            _httpMcApiServer.Broadcast(PacketPool<McApiOnEffectUpdatedPacket>.GetPacket().Set(bitmask, effect));
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
                    PacketPool<VcSetNameRequestPacket>.GetPacket().Set(networkEntity.Name));
                _liteNetServer.SendPacket(networkEntity.NetPeer,
                    PacketPool<VcSetServerMuteRequestPacket>.GetPacket().Set(networkEntity.ServerMuted));
                _liteNetServer.SendPacket(networkEntity.NetPeer,
                    PacketPool<VcSetServerDeafenRequestPacket>.GetPacket().Set(networkEntity.ServerDeafened));
                _liteNetServer.Broadcast(PacketPool<VcOnNetworkEntityCreatedPacket>.GetPacket().Set(networkEntity),
                    VcDeliveryMethod.Reliable, networkEntity.NetPeer);
                if (!EnableVisibilityDisplay)
                {
                    _liteNetServer.Broadcast(
                        PacketPool<VcSetEntityVisibilityRequestPacket>.GetPacket().Set(networkEntity.Id, true),
                        VcDeliveryMethod.Reliable, networkEntity.NetPeer);
                }
                
                _httpMcApiServer.Broadcast(PacketPool<McApiOnNetworkEntityCreatedPacket>.GetPacket().Set(networkEntity));

                //Send Effects
                foreach (var effect in _audioEffectSystem.AudioEffects)
                    _liteNetServer.SendPacket(networkEntity.NetPeer,
                        PacketPool<VcOnEffectUpdatedPacket>.GetPacket().Set(effect.Key, effect.Value));

                //Send other entities.
                foreach (var entity in _world.Entities.Where(x => x != networkEntity))
                {
                    if (entity is VoiceCraftNetworkEntity otherNetworkEntity)
                        _liteNetServer.SendPacket(networkEntity.NetPeer,
                            PacketPool<VcOnNetworkEntityCreatedPacket>.GetPacket().Set(otherNetworkEntity));
                    else
                        _liteNetServer.SendPacket(networkEntity.NetPeer,
                            PacketPool<VcOnEntityCreatedPacket>.GetPacket().Set(entity));

                    if (EnableVisibilityDisplay) continue;
                    _liteNetServer.SendPacket(networkEntity.NetPeer, PacketPool<VcSetEntityVisibilityRequestPacket>.GetPacket()
                        .Set(entity.Id, true));
                }

                AnsiConsole.MarkupLine(
                    $"[green]{Localizer.Get($"Events.Client.Connected:{networkEntity.UserGuid}")}[/]");
            }
            else
            {
                _liteNetServer.Broadcast(PacketPool<VcOnEntityCreatedPacket>.GetPacket().Set(newEntity));
                _httpMcApiServer.Broadcast(PacketPool<McApiOnEntityCreatedPacket>.GetPacket().Set(newEntity));
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
            var entityDestroyedPacket = PacketPool<VcOnEntityDestroyedPacket>.GetPacket().Set(entity.Id);
            if (entity is VoiceCraftNetworkEntity networkEntity)
            {
                _liteNetServer.Disconnect(networkEntity.NetPeer, "VoiceCraft.DisconnectReason.Kicked");
                AnsiConsole.MarkupLine(
                    $"[yellow]{Localizer.Get($"Events.Client.Disconnected:{networkEntity.UserGuid}")}[/]");
            }

            _liteNetServer.Broadcast(entityDestroyedPacket);
            _httpMcApiServer.Broadcast(PacketPool<McApiOnEntityDestroyedPacket>.GetPacket().Set(entity.Id));
        });
    }

    private void OnMcHttpMcApiPeerConnected(McApiNetPeer peer, string token)
    {
        _tasks.Enqueue(() =>
        {
            //Send Effects
            foreach (var effect in _audioEffectSystem.AudioEffects)
                _httpMcApiServer.SendPacket(peer,
                    PacketPool<McApiOnEffectUpdatedPacket>.GetPacket().Set(effect.Key, effect.Value));

            //Send other entities.
            foreach (var entity in _world.Entities)
                if (entity is VoiceCraftNetworkEntity otherNetworkEntity)
                    _httpMcApiServer.SendPacket(peer,
                        PacketPool<McApiOnNetworkEntityCreatedPacket>.GetPacket().Set(otherNetworkEntity));
                else
                    _httpMcApiServer.SendPacket(peer,
                        PacketPool<McApiOnEntityCreatedPacket>.GetPacket().Set(entity));

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

    //Data
    private void OnNetworkEntitySetTitle(string title, VoiceCraftNetworkEntity entity)
    {
        _tasks.Enqueue(() =>
        {
            _liteNetServer.SendPacket(entity.NetPeer, PacketPool<VcSetTitleRequestPacket>.GetPacket().Set(title));
        });
    }

    private void OnNetworkEntitySetDescription(string description, VoiceCraftNetworkEntity entity)
    {
        _tasks.Enqueue(() =>
        {
            _liteNetServer.SendPacket(entity.NetPeer,
                PacketPool<VcSetDescriptionRequestPacket>.GetPacket().Set(description));
        });
    }

    private void OnNetworkEntityServerMuteUpdated(bool muted, VoiceCraftNetworkEntity entity)
    {
        _tasks.Enqueue(() =>
        {
            _liteNetServer.SendPacket(entity.NetPeer, PacketPool<VcSetServerMuteRequestPacket>.GetPacket().Set(muted));
            _liteNetServer.Broadcast(PacketPool<VcOnEntityServerMuteUpdatedPacket>.GetPacket().Set(entity.Id, muted),
                VcDeliveryMethod.Reliable, entity.NetPeer);
            _httpMcApiServer.Broadcast(PacketPool<McApiOnEntityServerMuteUpdatedPacket>.GetPacket().Set(entity.Id, muted));
        });
    }

    private void OnNetworkEntityServerDeafenUpdated(bool deafened, VoiceCraftNetworkEntity entity)
    {
        _tasks.Enqueue(() =>
        {
            _liteNetServer.SendPacket(entity.NetPeer, PacketPool<VcSetServerDeafenRequestPacket>.GetPacket().Set(deafened));
            _liteNetServer.Broadcast(PacketPool<VcOnEntityServerDeafenUpdatedPacket>.GetPacket().Set(entity.Id, deafened),
                VcDeliveryMethod.Reliable, entity.NetPeer);
            _httpMcApiServer.Broadcast(PacketPool<McApiOnEntityServerDeafenUpdatedPacket>.GetPacket()
                .Set(entity.Id, deafened));
        });
    }

    private void OnEntityWorldIdUpdated(string worldId, VoiceCraftEntity entity)
    {
        _tasks.Enqueue(() =>
        {
            _httpMcApiServer.Broadcast(PacketPool<McApiOnEntityWorldIdUpdatedPacket>.GetPacket().Set(entity.Id, worldId));
        });
    }

    private void OnEntityNameUpdated(string name, VoiceCraftEntity entity)
    {
        _tasks.Enqueue(() =>
        {
            var packet = PacketPool<VcOnEntityNameUpdatedPacket>.GetPacket().Set(entity.Id, name);
            if (entity is VoiceCraftNetworkEntity networkEntity)
            {
                if (networkEntity.PositioningType == PositioningType.Server)
                    _liteNetServer.SendPacket(networkEntity.NetPeer, PacketPool<VcSetNameRequestPacket>.GetPacket().Set(name));
            }
            else
            {
                _liteNetServer.Broadcast(packet);
            }
            
            _httpMcApiServer.Broadcast(PacketPool<McApiOnEntityNameUpdatedPacket>.GetPacket().Set(entity.Id, name));
        });
    }

    private void OnEntityMuteUpdated(bool mute, VoiceCraftEntity entity)
    {
        _tasks.Enqueue(() =>
        {
            var packet = PacketPool<VcOnEntityMuteUpdatedPacket>.GetPacket().Set(entity.Id, mute);
            if (entity is VoiceCraftNetworkEntity networkEntity)
                _liteNetServer.Broadcast(packet, VcDeliveryMethod.Reliable, networkEntity.NetPeer);
            else
                _liteNetServer.Broadcast(packet);
            
            _httpMcApiServer.Broadcast(PacketPool<McApiOnEntityMuteUpdatedPacket>.GetPacket().Set(entity.Id, mute));
        });
    }

    private void OnEntityDeafenUpdated(bool deafen, VoiceCraftEntity entity)
    {
        _tasks.Enqueue(() =>
        {
            var packet = PacketPool<VcOnEntityDeafenUpdatedPacket>.GetPacket().Set(entity.Id, deafen);
            if (entity is VoiceCraftNetworkEntity networkEntity)
                _liteNetServer.Broadcast(packet, VcDeliveryMethod.Reliable, networkEntity.NetPeer);
            else
                _liteNetServer.Broadcast(packet);
            
            _httpMcApiServer.Broadcast(PacketPool<McApiOnEntityDeafenUpdatedPacket>.GetPacket().Set(entity.Id, deafen));
        });
    }

    private void OnEntityTalkBitmaskUpdated(ushort bitmask, VoiceCraftEntity entity)
    {
        _tasks.Enqueue(() =>
        {
            var packet = PacketPool<VcOnEntityTalkBitmaskUpdatedPacket>.GetPacket().Set(entity.Id, bitmask);
            if (entity is VoiceCraftNetworkEntity networkEntity)
            {
                _liteNetServer.SendPacket(networkEntity.NetPeer,
                    PacketPool<VcSetTalkBitmaskRequestPacket>.GetPacket().Set(bitmask));
                _liteNetServer.Broadcast(packet, VcDeliveryMethod.Reliable, networkEntity.NetPeer);
            }
            else
            {
                _liteNetServer.Broadcast(packet);
            }
            
            _httpMcApiServer.Broadcast(PacketPool<McApiOnEntityTalkBitmaskUpdatedPacket>.GetPacket()
                .Set(entity.Id, bitmask));
        });
    }

    private void OnEntityListenBitmaskUpdated(ushort bitmask, VoiceCraftEntity entity)
    {
        _tasks.Enqueue(() =>
        {
            var packet = PacketPool<VcOnEntityListenBitmaskUpdatedPacket>.GetPacket().Set(entity.Id, bitmask);
            if (entity is VoiceCraftNetworkEntity networkEntity)
            {
                _liteNetServer.SendPacket(networkEntity.NetPeer,
                    PacketPool<VcSetListenBitmaskRequestPacket>.GetPacket().Set(bitmask));
                _liteNetServer.Broadcast(packet, VcDeliveryMethod.Reliable, networkEntity.NetPeer);
            }
            else
            {
                _liteNetServer.Broadcast(packet);
            }
            
            _httpMcApiServer.Broadcast(PacketPool<McApiOnEntityListenBitmaskUpdatedPacket>.GetPacket()
                .Set(entity.Id, bitmask));
        });
    }

    private void OnEntityEffectBitmaskUpdated(ushort bitmask, VoiceCraftEntity entity)
    {
        _tasks.Enqueue(() =>
        {
            var packet = PacketPool<VcOnEntityEffectBitmaskUpdatedPacket>.GetPacket().Set(entity.Id, bitmask);
            if (entity is VoiceCraftNetworkEntity networkEntity)
            {
                _liteNetServer.SendPacket(networkEntity.NetPeer,
                    PacketPool<VcSetEffectBitmaskRequestPacket>.GetPacket().Set(bitmask));
                _liteNetServer.Broadcast(packet, VcDeliveryMethod.Reliable, networkEntity.NetPeer);
            }
            else
            {
                _liteNetServer.Broadcast(packet);
            }
            
            _httpMcApiServer.Broadcast(PacketPool<McApiOnEntityEffectBitmaskUpdatedPacket>.GetPacket()
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
                    PacketPool<VcSetPositionRequestPacket>.GetPacket().Set(position));

            foreach (var ve in entity.VisibleEntities)
            {
                if (ve is not VoiceCraftNetworkEntity visibleEntity) continue;
                var packet = PacketPool<VcOnEntityPositionUpdatedPacket>.GetPacket().Set(entity.Id, position);
                _liteNetServer.SendPacket(visibleEntity.NetPeer, packet);
            }
            
            _httpMcApiServer.Broadcast(PacketPool<McApiOnEntityPositionUpdatedPacket>.GetPacket()
                .Set(entity.Id, position));
        });
    }

    private void OnEntityRotationUpdated(Vector2 rotation, VoiceCraftEntity entity)
    {
        _tasks.Enqueue(() =>
        {
            if (entity is VoiceCraftNetworkEntity { PositioningType: PositioningType.Server } networkEntity)
                _liteNetServer.SendPacket(networkEntity.NetPeer,
                    PacketPool<VcSetRotationRequestPacket>.GetPacket().Set(rotation));

            foreach (var ve in entity.VisibleEntities)
            {
                if (ve is not VoiceCraftNetworkEntity visibleEntity) continue;
                var packet = PacketPool<VcOnEntityRotationUpdatedPacket>.GetPacket().Set(entity.Id, rotation);
                _liteNetServer.SendPacket(visibleEntity.NetPeer, packet);
            }
            
            _httpMcApiServer.Broadcast(PacketPool<McApiOnEntityRotationUpdatedPacket>.GetPacket()
                .Set(entity.Id, rotation));
        });
    }

    private void OnEntityCaveFactorUpdated(float caveFactor, VoiceCraftEntity entity)
    {
        _tasks.Enqueue(() =>
        {
            if (entity is VoiceCraftNetworkEntity { PositioningType: PositioningType.Server } networkEntity)
                _liteNetServer.SendPacket(networkEntity.NetPeer,
                    PacketPool<VcSetCaveFactorRequest>.GetPacket().Set(caveFactor));

            foreach (var ve in entity.VisibleEntities)
            {
                if (ve is not VoiceCraftNetworkEntity visibleEntity) continue;
                var packet = PacketPool<VcOnEntityCaveFactorUpdatedPacket>.GetPacket().Set(entity.Id, caveFactor);
                _liteNetServer.SendPacket(visibleEntity.NetPeer, packet);
            }
            
            _httpMcApiServer.Broadcast(PacketPool<McApiOnEntityCaveFactorUpdatedPacket>.GetPacket()
                .Set(entity.Id, caveFactor));
        });
    }

    private void OnEntityMuffleFactorUpdated(float muffleFactor, VoiceCraftEntity entity)
    {
        _tasks.Enqueue(() =>
        {
            if (entity is VoiceCraftNetworkEntity { PositioningType: PositioningType.Server } networkEntity)
                _liteNetServer.SendPacket(networkEntity.NetPeer,
                    PacketPool<VcSetMuffleFactorRequest>.GetPacket().Set(muffleFactor));

            foreach (var ve in entity.VisibleEntities)
            {
                if (ve is not VoiceCraftNetworkEntity visibleEntity) continue;
                var packet = PacketPool<VcOnEntityMuffleFactorUpdatedPacket>.GetPacket().Set(entity.Id, muffleFactor);
                _liteNetServer.SendPacket(visibleEntity.NetPeer, packet);
            }
            
            _httpMcApiServer.Broadcast(PacketPool<McApiOnEntityMuffleFactorUpdatedPacket>.GetPacket()
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
                    var visibilityPacket = PacketPool<VcSetEntityVisibilityRequestPacket>.GetPacket()
                        .Set(entity.Id, true);
                    _liteNetServer.SendPacket(networkEntity.NetPeer, visibilityPacket);
                }

                var positionPacket = PacketPool<VcOnEntityPositionUpdatedPacket>.GetPacket()
                    .Set(entity.Id, entity.Position);
                var rotationPacket = PacketPool<VcOnEntityRotationUpdatedPacket>.GetPacket()
                    .Set(entity.Id, entity.Rotation);
                var caveFactorPacket = PacketPool<VcOnEntityCaveFactorUpdatedPacket>.GetPacket()
                    .Set(entity.Id, entity.CaveFactor);
                var muffleFactorPacket = PacketPool<VcOnEntityMuffleFactorUpdatedPacket>.GetPacket()
                    .Set(entity.Id, entity.MuffleFactor);

                _liteNetServer.SendPacket(networkEntity.NetPeer, positionPacket);
                _liteNetServer.SendPacket(networkEntity.NetPeer, rotationPacket);
                _liteNetServer.SendPacket(networkEntity.NetPeer, caveFactorPacket);
                _liteNetServer.SendPacket(networkEntity.NetPeer, muffleFactorPacket);
            }
            
            _httpMcApiServer.Broadcast(PacketPool<McApiOnEntityVisibilityUpdatedPacket>.GetPacket()
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
                        PacketPool<VcSetEntityVisibilityRequestPacket>.GetPacket().Set(entity.Id));
                }
            }
            
            _httpMcApiServer.Broadcast(PacketPool<McApiOnEntityVisibilityUpdatedPacket>.GetPacket()
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
                var packet = PacketPool<VcOnEntityAudioReceivedPacket>.GetPacket()
                    .Set(entity.Id, timestamp, frameLoudness, buffer.Length, buffer);
                _liteNetServer.SendPacket(visibleEntity.NetPeer, packet, VcDeliveryMethod.Unreliable);
            }
            
            _httpMcApiServer.Broadcast(PacketPool<McApiOnEntityAudioReceivedPacket>.GetPacket()
                .Set(entity.Id, timestamp, frameLoudness));
        });
    }

    #endregion
}