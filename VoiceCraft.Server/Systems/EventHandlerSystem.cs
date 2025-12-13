using System.Collections.Concurrent;
using System.Numerics;
using LiteNetLib;
using Spectre.Console;
using VoiceCraft.Core;
using VoiceCraft.Core.Interfaces;
using VoiceCraft.Core.Locales;
using VoiceCraft.Core.Network;
using VoiceCraft.Core.Network.McApiPackets.Event;
using VoiceCraft.Core.Network.VcPackets.Event;
using VoiceCraft.Core.Network.VcPackets.Request;
using VoiceCraft.Core.World;
using VoiceCraft.Server.Servers;

namespace VoiceCraft.Server.Systems;

public class EventHandlerSystem : IDisposable
{
    private readonly AudioEffectSystem _audioEffectSystem;
    private readonly VoiceCraftServer _server;
    private readonly McWssServer _mcWssServer;
    private readonly McHttpServer _mcHttpServer;
    private readonly VoiceCraftWorld _world;
    private readonly ConcurrentQueue<Action> _tasks = [];

    public EventHandlerSystem(
        VoiceCraftServer server,
        McWssServer mcWssServer,
        McHttpServer mcHttpServer,
        VoiceCraftWorld world,
        AudioEffectSystem audioEffectSystem)
    {
        _server = server;
        _mcWssServer = mcWssServer;
        _mcHttpServer = mcHttpServer;
        _world = world;
        _audioEffectSystem = audioEffectSystem;

        _world.OnEntityCreated += OnEntityCreated;
        _world.OnEntityDestroyed += OnEntityDestroyed;
        _audioEffectSystem.OnEffectSet += OnAudioEffectSet;
        _mcWssServer.OnPeerConnected += OnMcApiPeerConnected;
        _mcWssServer.OnPeerDisconnected += OnMcApiPeerDisconnected;
        _mcHttpServer.OnPeerConnected += OnMcApiPeerConnected;
        _mcHttpServer.OnPeerDisconnected += OnMcApiPeerDisconnected;
    }

    public void Dispose()
    {
        _world.OnEntityCreated -= OnEntityCreated;
        _world.OnEntityDestroyed -= OnEntityDestroyed;
        _audioEffectSystem.OnEffectSet -= OnAudioEffectSet;
        _mcWssServer.OnPeerConnected -= OnMcApiPeerConnected;
        _mcWssServer.OnPeerDisconnected -= OnMcApiPeerDisconnected;
        _mcHttpServer.OnPeerConnected -= OnMcApiPeerConnected;
        _mcHttpServer.OnPeerDisconnected -= OnMcApiPeerDisconnected;
        GC.SuppressFinalize(this);
    }

    public void Update()
    {
        while (_tasks.TryDequeue(out var result))
            result.Invoke();
    }

    #region Audio Effect Events

    private void OnAudioEffectSet(ushort bitmask, IAudioEffect? effect)
    {
        _tasks.Enqueue(() =>
        {
            var packet = PacketPool<VcOnEffectUpdatedPacket>.GetPacket().Set(bitmask, effect);
            _server.Broadcast(packet);
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
                _server.SendPacket(networkEntity.NetPeer,
                    PacketPool<VcSetNameRequestPacket>.GetPacket().Set(networkEntity.Name));
                _server.Broadcast(PacketPool<VcOnNetworkEntityCreatedPacket>.GetPacket().Set(networkEntity),
                    DeliveryMethod.ReliableOrdered, networkEntity.NetPeer);
                _mcWssServer.Broadcast(PacketPool<McApiOnNetworkEntityCreatedPacket>.GetPacket().Set(networkEntity));
                _mcHttpServer.Broadcast(PacketPool<McApiOnNetworkEntityCreatedPacket>.GetPacket().Set(networkEntity));

                //Send Effects
                foreach (var effect in _audioEffectSystem.Effects)
                    _server.SendPacket(networkEntity.NetPeer,
                        PacketPool<VcOnEffectUpdatedPacket>.GetPacket().Set(effect.Key, effect.Value));

                //Send other entities.
                foreach (var entity in _world.Entities.Where(x => x != networkEntity))
                {
                    if (entity is VoiceCraftNetworkEntity otherNetworkEntity)
                        _server.SendPacket(networkEntity.NetPeer,
                            PacketPool<VcOnNetworkEntityCreatedPacket>.GetPacket().Set(otherNetworkEntity));
                    else
                        _server.SendPacket(networkEntity.NetPeer,
                            PacketPool<VcOnEntityCreatedPacket>.GetPacket().Set(entity));
                }

                AnsiConsole.MarkupLine(
                    $"[green]{Localizer.Get($"Events.Client.Connected:{networkEntity.UserGuid}")}[/]");
            }
            else
            {
                _server.Broadcast(PacketPool<VcOnEntityCreatedPacket>.GetPacket().Set(newEntity));
                _mcWssServer.Broadcast(PacketPool<McApiOnEntityCreatedPacket>.GetPacket().Set(newEntity));
                _mcHttpServer.Broadcast(PacketPool<McApiOnEntityCreatedPacket>.GetPacket().Set(newEntity));
            }
        });
    }

    private void OnEntityDestroyed(VoiceCraftEntity entity)
    {
        if (entity is VoiceCraftNetworkEntity netEntity)
        {
            netEntity.OnSetTitle -= OnNetworkEntitySetTitle;
            netEntity.OnSetDescription -= OnNetworkEntitySetDescription;
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
                _server.DisconnectPeer(networkEntity.NetPeer, "VoiceCraft.DisconnectReason.Kicked");
                AnsiConsole.MarkupLine(
                    $"[yellow]{Localizer.Get($"Events.Client.Disconnected:{networkEntity.UserGuid}")}[/]");
            }

            _server.Broadcast(entityDestroyedPacket);
            _mcWssServer.Broadcast(PacketPool<McApiOnEntityDestroyedPacket>.GetPacket().Set(entity.Id));
            _mcHttpServer.Broadcast(PacketPool<McApiOnEntityDestroyedPacket>.GetPacket().Set(entity.Id));
        });
    }

    private void OnMcApiPeerConnected(McApiNetPeer peer)
    {
        _tasks.Enqueue(() =>
        {
            //Send Effects
            foreach (var effect in _audioEffectSystem.Effects)
            {
                _mcWssServer.SendPacket(peer,
                    PacketPool<McApiOnEffectUpdatedPacket>.GetPacket().Set(effect.Key, effect.Value));
                _mcHttpServer.SendPacket(peer,
                    PacketPool<McApiOnEffectUpdatedPacket>.GetPacket().Set(effect.Key, effect.Value));
            }


            //Send other entities.
            foreach (var entity in _world.Entities)
            {
                if (entity is VoiceCraftNetworkEntity otherNetworkEntity)
                {
                    _mcWssServer.SendPacket(peer,
                        PacketPool<McApiOnNetworkEntityCreatedPacket>.GetPacket().Set(otherNetworkEntity));
                    _mcHttpServer.SendPacket(peer,
                        PacketPool<McApiOnNetworkEntityCreatedPacket>.GetPacket().Set(otherNetworkEntity));
                }
                else
                {
                    _mcWssServer.SendPacket(peer,
                        PacketPool<McApiOnEntityCreatedPacket>.GetPacket().Set(entity));
                    _mcHttpServer.SendPacket(peer,
                        PacketPool<McApiOnEntityCreatedPacket>.GetPacket().Set(entity));
                }
            }

            AnsiConsole.MarkupLine($"[green]{Localizer.Get($"Events.McApi.Client.Connected:{peer.Token}")}[/]");
        });
    }

    private void OnMcApiPeerDisconnected(McApiNetPeer peer)
    {
        _tasks.Enqueue(() =>
        {
            AnsiConsole.MarkupLine($"[yellow]{Localizer.Get($"Events.McApi.Client.Disconnected:{peer.Token}")}[/]");
        });
    }

    //Data
    private void OnNetworkEntitySetTitle(string title, VoiceCraftNetworkEntity entity)
    {
        _tasks.Enqueue(() =>
        {
            _server.SendPacket(entity.NetPeer, PacketPool<VcSetTitleRequestPacket>.GetPacket().Set(title));
        });
    }

    private void OnNetworkEntitySetDescription(string description, VoiceCraftNetworkEntity entity)
    {
        _tasks.Enqueue(() =>
        {
            _server.SendPacket(entity.NetPeer,
                PacketPool<VcSetDescriptionRequestPacket>.GetPacket().Set(description));
        });
    }

    private void OnEntityWorldIdUpdated(string worldId, VoiceCraftEntity entity)
    {
        _tasks.Enqueue(() =>
        {
            _mcWssServer.Broadcast(PacketPool<McApiOnWorldIdUpdatedPacket>.GetPacket().Set(entity.Id, worldId));
            _mcHttpServer.Broadcast(PacketPool<McApiOnWorldIdUpdatedPacket>.GetPacket().Set(entity.Id, worldId));
        });
    }

    private void OnEntityNameUpdated(string name, VoiceCraftEntity entity)
    {
        _tasks.Enqueue(() =>
        {
            var packet = PacketPool<VcOnEntityNameUpdatedPacket>.GetPacket().Set(entity.Id, name);
            if (entity is VoiceCraftNetworkEntity networkEntity)
            {
                _server.SendPacket(networkEntity.NetPeer, PacketPool<VcSetNameRequestPacket>.GetPacket().Set(name));
                _server.Broadcast(packet, DeliveryMethod.ReliableOrdered, networkEntity.NetPeer);
            }
            else
                _server.Broadcast(packet);

            _mcWssServer.Broadcast(PacketPool<McApiOnNameUpdatedPacket>.GetPacket().Set(entity.Id, name));
            _mcHttpServer.Broadcast(PacketPool<McApiOnNameUpdatedPacket>.GetPacket().Set(entity.Id, name));
        });
    }

    private void OnEntityMuteUpdated(bool mute, VoiceCraftEntity entity)
    {
        _tasks.Enqueue(() =>
        {
            var packet = PacketPool<VcOnEntityMuteUpdatedPacket>.GetPacket().Set(entity.Id, mute);
            if (entity is VoiceCraftNetworkEntity networkEntity)
            {
                _server.SendPacket(networkEntity.NetPeer, PacketPool<VcSetMuteRequestPacket>.GetPacket().Set(mute));
                _server.Broadcast(packet, DeliveryMethod.ReliableOrdered, networkEntity.NetPeer);
            }
            else
                _server.Broadcast(packet);

            _mcWssServer.Broadcast(PacketPool<McApiOnMuteUpdatedPacket>.GetPacket().Set(entity.Id, mute));
            _mcHttpServer.Broadcast(PacketPool<McApiOnMuteUpdatedPacket>.GetPacket().Set(entity.Id, mute));
        });
    }

    private void OnEntityDeafenUpdated(bool deafen, VoiceCraftEntity entity)
    {
        _tasks.Enqueue(() =>
        {
            var packet = PacketPool<VcOnEntityDeafenUpdatedPacket>.GetPacket().Set(entity.Id, deafen);
            if (entity is VoiceCraftNetworkEntity networkEntity)
            {
                _server.SendPacket(networkEntity.NetPeer, PacketPool<VcSetDeafenRequestPacket>.GetPacket().Set(deafen));
                _server.Broadcast(packet, DeliveryMethod.ReliableOrdered, networkEntity.NetPeer);
            }
            else
                _server.Broadcast(packet);

            _mcWssServer.Broadcast(PacketPool<McApiOnDeafenUpdatedPacket>.GetPacket().Set(entity.Id, deafen));
            _mcHttpServer.Broadcast(PacketPool<McApiOnDeafenUpdatedPacket>.GetPacket().Set(entity.Id, deafen));
        });
    }

    private void OnEntityTalkBitmaskUpdated(ushort bitmask, VoiceCraftEntity entity)
    {
        _tasks.Enqueue(() =>
        {
            var packet = PacketPool<VcOnEntityTalkBitmaskUpdatedPacket>.GetPacket().Set(entity.Id, bitmask);
            if (entity is VoiceCraftNetworkEntity networkEntity)
            {
                _server.SendPacket(networkEntity.NetPeer,
                    PacketPool<VcSetTalkBitmaskRequestPacket>.GetPacket().Set(bitmask));
                _server.Broadcast(packet, DeliveryMethod.ReliableOrdered, networkEntity.NetPeer);
            }
            else
                _server.Broadcast(packet);

            _mcWssServer.Broadcast(PacketPool<McApiOnTalkBitmaskUpdatedPacket>.GetPacket().Set(entity.Id, bitmask));
            _mcHttpServer.Broadcast(PacketPool<McApiOnTalkBitmaskUpdatedPacket>.GetPacket().Set(entity.Id, bitmask));
        });
    }

    private void OnEntityListenBitmaskUpdated(ushort bitmask, VoiceCraftEntity entity)
    {
        _tasks.Enqueue(() =>
        {
            var packet = PacketPool<VcOnEntityListenBitmaskUpdatedPacket>.GetPacket().Set(entity.Id, bitmask);
            if (entity is VoiceCraftNetworkEntity networkEntity)
            {
                _server.SendPacket(networkEntity.NetPeer,
                    PacketPool<VcSetListenBitmaskRequestPacket>.GetPacket().Set(bitmask));
                _server.Broadcast(packet, DeliveryMethod.ReliableOrdered, networkEntity.NetPeer);
            }
            else
                _server.Broadcast(packet);

            _mcWssServer.Broadcast(PacketPool<McApiOnListenBitmaskUpdatedPacket>.GetPacket().Set(entity.Id, bitmask));
            _mcHttpServer.Broadcast(PacketPool<McApiOnListenBitmaskUpdatedPacket>.GetPacket().Set(entity.Id, bitmask));
        });
    }

    private void OnEntityEffectBitmaskUpdated(ushort bitmask, VoiceCraftEntity entity)
    {
        _tasks.Enqueue(() =>
        {
            var packet = PacketPool<VcOnEntityEffectBitmaskUpdatedPacket>.GetPacket().Set(entity.Id, bitmask);
            if (entity is VoiceCraftNetworkEntity networkEntity)
            {
                _server.SendPacket(networkEntity.NetPeer,
                    PacketPool<VcSetEffectBitmaskRequestPacket>.GetPacket().Set(bitmask));
                _server.Broadcast(packet, DeliveryMethod.ReliableOrdered, networkEntity.NetPeer);
            }
            else
                _server.Broadcast(packet);

            _mcWssServer.Broadcast(PacketPool<McApiOnEffectBitmaskUpdatedPacket>.GetPacket().Set(entity.Id, bitmask));
            _mcHttpServer.Broadcast(PacketPool<McApiOnEffectBitmaskUpdatedPacket>.GetPacket().Set(entity.Id, bitmask));
        });
    }

    //Properties
    private void OnEntityPositionUpdated(Vector3 position, VoiceCraftEntity entity)
    {
        _tasks.Enqueue(() =>
        {
            if (entity is VoiceCraftNetworkEntity networkEntity)
            {
                _server.SendPacket(networkEntity.NetPeer,
                    PacketPool<VcSetPositionRequestPacket>.GetPacket().Set(position));
            }

            foreach (var ve in entity.VisibleEntities)
            {
                if (ve is not VoiceCraftNetworkEntity visibleEntity) continue;
                var packet = PacketPool<VcOnEntityPositionUpdatedPacket>.GetPacket().Set(entity.Id, position);
                _server.SendPacket(visibleEntity.NetPeer, packet);
            }

            _mcWssServer.Broadcast(PacketPool<McApiOnPositionUpdatedPacket>.GetPacket().Set(entity.Id, position));
            _mcHttpServer.Broadcast(PacketPool<McApiOnPositionUpdatedPacket>.GetPacket().Set(entity.Id, position));
        });
    }

    private void OnEntityRotationUpdated(Vector2 rotation, VoiceCraftEntity entity)
    {
        _tasks.Enqueue(() =>
        {
            if (entity is VoiceCraftNetworkEntity networkEntity)
            {
                _server.SendPacket(networkEntity.NetPeer,
                    PacketPool<VcSetRotationRequestPacket>.GetPacket().Set(rotation));
            }

            foreach (var ve in entity.VisibleEntities)
            {
                if (ve is not VoiceCraftNetworkEntity visibleEntity) continue;
                var packet = PacketPool<VcOnEntityRotationUpdatedPacket>.GetPacket().Set(entity.Id, rotation);
                _server.SendPacket(visibleEntity.NetPeer, packet);
            }

            _mcWssServer.Broadcast(PacketPool<McApiOnRotationUpdatedPacket>.GetPacket().Set(entity.Id, rotation));
            _mcHttpServer.Broadcast(PacketPool<McApiOnRotationUpdatedPacket>.GetPacket().Set(entity.Id, rotation));
        });
    }

    private void OnEntityCaveFactorUpdated(float caveFactor, VoiceCraftEntity entity)
    {
        _tasks.Enqueue(() =>
        {
            if (entity is VoiceCraftNetworkEntity networkEntity)
            {
                _server.SendPacket(networkEntity.NetPeer,
                    PacketPool<VcSetCaveFactorRequest>.GetPacket().Set(caveFactor));
            }

            foreach (var ve in entity.VisibleEntities)
            {
                if (ve is not VoiceCraftNetworkEntity visibleEntity) continue;
                var packet = PacketPool<VcOnEntityCaveFactorUpdatedPacket>.GetPacket().Set(entity.Id, caveFactor);
                _server.SendPacket(visibleEntity.NetPeer, packet);
            }

            _mcWssServer.Broadcast(PacketPool<McApiOnEntityCaveFactorUpdatedPacket>.GetPacket()
                .Set(entity.Id, caveFactor));
            _mcHttpServer.Broadcast(PacketPool<McApiOnEntityCaveFactorUpdatedPacket>.GetPacket()
                .Set(entity.Id, caveFactor));
        });
    }

    private void OnEntityMuffleFactorUpdated(float muffleFactor, VoiceCraftEntity entity)
    {
        _tasks.Enqueue(() =>
        {
            if (entity is VoiceCraftNetworkEntity networkEntity)
            {
                _server.SendPacket(networkEntity.NetPeer,
                    PacketPool<VcSetMuffleFactorRequest>.GetPacket().Set(muffleFactor));
            }

            foreach (var ve in entity.VisibleEntities)
            {
                if (ve is not VoiceCraftNetworkEntity visibleEntity) continue;
                var packet = PacketPool<VcOnEntityMuffleFactorUpdatedPacket>.GetPacket().Set(entity.Id, muffleFactor);
                _server.SendPacket(visibleEntity.NetPeer, packet);
            }

            _mcWssServer.Broadcast(PacketPool<McApiOnEntityMuffleFactorUpdatedPacket>.GetPacket()
                .Set(entity.Id, muffleFactor));
            _mcHttpServer.Broadcast(PacketPool<McApiOnEntityMuffleFactorUpdatedPacket>.GetPacket()
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
                var visibilityPacket = PacketPool<VcSetEntityVisibilityRequestPacket>.GetPacket()
                    .Set(entity.Id, true);
                var positionPacket = PacketPool<VcOnEntityPositionUpdatedPacket>.GetPacket()
                    .Set(entity.Id, entity.Position);
                var rotationPacket = PacketPool<VcOnEntityRotationUpdatedPacket>.GetPacket()
                    .Set(entity.Id, entity.Rotation);
                var caveFactorPacket = PacketPool<VcOnEntityCaveFactorUpdatedPacket>.GetPacket()
                    .Set(entity.Id, entity.CaveFactor);
                var muffleFactorPacket = PacketPool<VcOnEntityMuffleFactorUpdatedPacket>.GetPacket()
                    .Set(entity.Id, entity.MuffleFactor);

                _server.SendPacket(networkEntity.NetPeer, visibilityPacket);
                _server.SendPacket(networkEntity.NetPeer, positionPacket);
                _server.SendPacket(networkEntity.NetPeer, rotationPacket);
                _server.SendPacket(networkEntity.NetPeer, caveFactorPacket);
                _server.SendPacket(networkEntity.NetPeer, muffleFactorPacket);
            }

            _mcWssServer.Broadcast(PacketPool<McApiOnEntityVisibilityUpdatedPacket>.GetPacket()
                .Set(entity.Id, addedEntity.Id, true));
            _mcHttpServer.Broadcast(PacketPool<McApiOnEntityVisibilityUpdatedPacket>.GetPacket()
                .Set(entity.Id, addedEntity.Id, true));
        });
    }

    private void OnEntityVisibleEntityRemoved(VoiceCraftEntity removedEntity, VoiceCraftEntity entity)
    {
        _tasks.Enqueue(() =>
        {
            if (removedEntity is VoiceCraftNetworkEntity networkEntity)
                _server.SendPacket(networkEntity.NetPeer,
                    PacketPool<VcSetEntityVisibilityRequestPacket>.GetPacket().Set(entity.Id));

            _mcWssServer.Broadcast(PacketPool<McApiOnEntityVisibilityUpdatedPacket>.GetPacket()
                .Set(entity.Id, removedEntity.Id));
            _mcHttpServer.Broadcast(PacketPool<McApiOnEntityVisibilityUpdatedPacket>.GetPacket()
                .Set(entity.Id, removedEntity.Id));
        });
    }

    private void OnEntityAudioReceived(byte[] data, ushort timestamp, float frameLoudness, VoiceCraftEntity entity)
    {
        _tasks.Enqueue(() =>
        {
            foreach (var ve in entity.VisibleEntities)
            {
                if (ve is not VoiceCraftNetworkEntity visibleEntity || ve == entity || visibleEntity.Deafened) continue;
                var packet = PacketPool<VcOnEntityAudioReceivedPacket>.GetPacket()
                    .Set(entity.Id, timestamp, frameLoudness, data.Length, data);
                _server.SendPacket(visibleEntity.NetPeer, packet);
            }

            _mcWssServer.Broadcast(PacketPool<McApiOnEntityAudioReceivedPacket>.GetPacket()
                .Set(entity.Id, timestamp, frameLoudness));
            _mcHttpServer.Broadcast(PacketPool<McApiOnEntityAudioReceivedPacket>.GetPacket()
                .Set(entity.Id, timestamp, frameLoudness));
        });
    }

    #endregion
}