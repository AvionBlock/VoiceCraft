using System.Collections.Concurrent;
using System.Numerics;
using VoiceCraft.Core.Interfaces;
using VoiceCraft.Core.Network.Packets;
using VoiceCraft.Core.World;
using VoiceCraft.Server.Servers;

namespace VoiceCraft.Server.Systems;

public class EventHandlerSystem : IDisposable
{
    private readonly AudioEffectSystem _audioEffectSystem;
    private readonly VoiceCraftServer _server;
    private readonly ConcurrentQueue<Action> _tasks = [];
    private readonly VoiceCraftWorld _world;

    public EventHandlerSystem(VoiceCraftServer server, VoiceCraftWorld world, AudioEffectSystem audioEffectSystem)
    {
        _server = server;
        _world = world;
        _audioEffectSystem = audioEffectSystem;

        _world.OnEntityCreated += OnEntityCreated;
        _world.OnEntityDestroyed += OnEntityDestroyed;
        _audioEffectSystem.OnEffectSet += OnAudioEffectSet;
    }

    public void Dispose()
    {
        _world.OnEntityCreated -= OnEntityCreated;
        _world.OnEntityDestroyed -= OnEntityDestroyed;
        _audioEffectSystem.OnEffectSet -= OnAudioEffectSet;
        GC.SuppressFinalize(this);
    }

    public void Update()
    {
        Parallel.For(0, _tasks.Count, _ =>
        {
            if(_tasks.TryDequeue(out var result))
                result.Invoke();
        });
    }

    #region Audio Effect Events

    private void OnAudioEffectSet(ushort bitmask, IAudioEffect? effect)
    {
        _tasks.Enqueue(() =>
        {
            var packet = new SetEffectPacket(bitmask, effect);
            _server.Broadcast(packet);
        });
    }

    #endregion

    #region Entity Events

    //World
    private void OnEntityCreated(VoiceCraftEntity newEntity)
    {
        if (newEntity is VoiceCraftNetworkEntity networkEntity)
        {
            _server.SendPacket(networkEntity.NetPeer, new SetIdPacket(networkEntity.Id));
            _server.Broadcast(new NetworkEntityCreatedPacket(networkEntity.Id, networkEntity.Name, networkEntity.Muted,
                networkEntity.Deafened,
                networkEntity.UserGuid));

            //Send Effects
            foreach (var effect in _audioEffectSystem.Effects)
                _server.SendPacket(networkEntity.NetPeer, new SetEffectPacket(effect.Key, effect.Value));

            //Send other entities.
            foreach (var entity in _world.Entities)
            {
                if (entity == networkEntity) continue;
                if (entity is VoiceCraftNetworkEntity otherNetworkEntity)
                    _server.SendPacket(networkEntity.NetPeer, new NetworkEntityCreatedPacket(entity.Id, entity.Name,
                        entity.Muted,
                        entity.Deafened, otherNetworkEntity.UserGuid));
                else
                    _server.SendPacket(networkEntity.NetPeer,
                        new EntityCreatedPacket(entity.Id, entity.Name, entity.Muted, entity.Deafened));
            }
        }
        else
        {
            _server.Broadcast(
                new EntityCreatedPacket(newEntity.Id, newEntity.Name, newEntity.Muted, newEntity.Deafened));
        }

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
    }

    private void OnEntityDestroyed(VoiceCraftEntity entity)
    {
        var entityDestroyedPacket = new EntityDestroyedPacket(entity.Id);
        if (entity is VoiceCraftNetworkEntity networkEntity)
            _server.DisconnectPeer(networkEntity.NetPeer, new LogoutPacket("VoiceCraft.DisconnectReason.Forced"));
        _server.Broadcast(entityDestroyedPacket);

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
    }

    //Data
    private void OnEntityNameUpdated(string name, VoiceCraftEntity entity)
    {
        _tasks.Enqueue(() =>
        {
            var packet = new SetNamePacket(entity.Id, name);
            _server.Broadcast(packet);
        });
    }

    private void OnEntityMuteUpdated(bool mute, VoiceCraftEntity entity)
    {
        _tasks.Enqueue(() =>
        {
            var packet = new SetMutePacket(entity.Id, mute);
            if (entity is VoiceCraftNetworkEntity networkEntity)
                _server.Broadcast(packet, excludes: networkEntity.NetPeer);
            else
                _server.Broadcast(packet);
        });
    }

    private void OnEntityDeafenUpdated(bool deafen, VoiceCraftEntity entity)
    {
        _tasks.Enqueue(() =>
        {
            var packet = new SetDeafenPacket(entity.Id, deafen);
            if (entity is VoiceCraftNetworkEntity networkEntity)
                _server.Broadcast(packet, excludes: networkEntity.NetPeer);
            else
                _server.Broadcast(packet);
        });
    }

    private void OnEntityTalkBitmaskUpdated(ushort bitmask, VoiceCraftEntity entity)
    {
        _tasks.Enqueue(() =>
        {
            var packet = new SetTalkBitmaskPacket(entity.Id, bitmask);
            var visibleNetworkEntities = entity.VisibleEntities.OfType<VoiceCraftNetworkEntity>();
            foreach (var visibleEntity in visibleNetworkEntities) _server.SendPacket(visibleEntity.NetPeer, packet);
        });
    }

    private void OnEntityListenBitmaskUpdated(ushort bitmask, VoiceCraftEntity entity)
    {
        _tasks.Enqueue(() =>
        {
            var packet = new SetListenBitmaskPacket(entity.Id, bitmask);
            var visibleNetworkEntities = entity.VisibleEntities.OfType<VoiceCraftNetworkEntity>();
            foreach (var visibleEntity in visibleNetworkEntities) _server.SendPacket(visibleEntity.NetPeer, packet);
        });
    }

    private void OnEntityEffectBitmaskUpdated(ushort bitmask, VoiceCraftEntity entity)
    {
        _tasks.Enqueue(() =>
        {
            var packet = new SetEffectBitmaskPacket(entity.Id, bitmask);
            var visibleNetworkEntities = entity.VisibleEntities.OfType<VoiceCraftNetworkEntity>();
            foreach (var visibleEntity in visibleNetworkEntities) _server.SendPacket(visibleEntity.NetPeer, packet);
        });
    }

    private void OnEntityPositionUpdated(Vector3 position, VoiceCraftEntity entity)
    {
        _tasks.Enqueue(() =>
        {
            var packet = new SetPositionPacket(entity.Id, position);
            var visibleNetworkEntities = entity.VisibleEntities.OfType<VoiceCraftNetworkEntity>();
            foreach (var visibleEntity in visibleNetworkEntities) _server.SendPacket(visibleEntity.NetPeer, packet);
        });
    }

    private void OnEntityRotationUpdated(Vector2 rotation, VoiceCraftEntity entity)
    {
        _tasks.Enqueue(() =>
        {
            //Only send updates to visible entities.
            var packet = new SetRotationPacket(entity.Id, rotation);
            var visibleNetworkEntities = entity.VisibleEntities.OfType<VoiceCraftNetworkEntity>();
            foreach (var visibleEntity in visibleNetworkEntities) _server.SendPacket(visibleEntity.NetPeer, packet);
        });
    }

    private void OnEntityCaveFactorUpdated(float caveFactor, VoiceCraftEntity entity)
    {
        _tasks.Enqueue(() =>
        {
            //Only send updates to visible entities.
            var packet = new SetCaveFactorPacket(entity.Id, caveFactor);
            var visibleNetworkEntities = entity.VisibleEntities.OfType<VoiceCraftNetworkEntity>();
            foreach (var visibleEntity in visibleNetworkEntities) _server.SendPacket(visibleEntity.NetPeer, packet);
        });
    }

    private void OnEntityMuffleFactorUpdated(float muffleFactor, VoiceCraftEntity entity)
    {
        _tasks.Enqueue(() =>
        {
            //Only send updates to visible entities.
            var packet = new SetMuffleFactorPacket(entity.Id, muffleFactor);
            var visibleNetworkEntities = entity.VisibleEntities.OfType<VoiceCraftNetworkEntity>();
            foreach (var visibleEntity in visibleNetworkEntities) _server.SendPacket(visibleEntity.NetPeer, packet);
        });
    }

    //Properties

    private void OnEntityVisibleEntityAdded(VoiceCraftEntity addedEntity, VoiceCraftEntity entity)
    {
        if (addedEntity is not VoiceCraftNetworkEntity networkEntity) return;
        _tasks.Enqueue(() =>
        {
            var visibilityPacket = new SetVisibilityPacket(entity.Id, true);
            var talkBitmaskPacket = new SetTalkBitmaskPacket(entity.Id, entity.TalkBitmask);
            var listenBitmaskPacket = new SetListenBitmaskPacket(entity.Id, entity.ListenBitmask);
            var effectBitmaskPacket = new SetEffectBitmaskPacket(entity.Id, entity.EffectBitmask);
            var positionPacket = new SetPositionPacket(entity.Id, entity.Position);
            var rotationPacket = new SetRotationPacket(entity.Id, entity.Rotation);
            var caveFactorPacket = new SetCaveFactorPacket(entity.Id, entity.CaveFactor);
            var muffleFactorPacket = new SetMuffleFactorPacket(entity.Id, entity.MuffleFactor);

            _server.SendPacket(networkEntity.NetPeer, visibilityPacket);
            _server.SendPacket(networkEntity.NetPeer, talkBitmaskPacket);
            _server.SendPacket(networkEntity.NetPeer, listenBitmaskPacket);
            _server.SendPacket(networkEntity.NetPeer, effectBitmaskPacket);
            _server.SendPacket(networkEntity.NetPeer, positionPacket);
            _server.SendPacket(networkEntity.NetPeer, rotationPacket);
            _server.SendPacket(networkEntity.NetPeer, caveFactorPacket);
            _server.SendPacket(networkEntity.NetPeer, muffleFactorPacket);
        });
    }

    private void OnEntityVisibleEntityRemoved(VoiceCraftEntity removedEntity, VoiceCraftEntity entity)
    {
        if (removedEntity is not VoiceCraftNetworkEntity networkEntity) return;
        _tasks.Enqueue(() => { _server.SendPacket(networkEntity.NetPeer, new SetVisibilityPacket(entity.Id)); });
    }

    private void OnEntityAudioReceived(byte[] data, ushort timestamp, float frameLoudness, VoiceCraftEntity entity)
    {
        _tasks.Enqueue(() =>
        {
            //Only send updates to visible entities.
            var packet = new AudioPacket(entity.Id, timestamp, frameLoudness, data.Length, data);
            var visibleNetworkEntities =
                entity.VisibleEntities.OfType<VoiceCraftNetworkEntity>()
                    .Where(x => x != entity && !x.Deafened);
            foreach (var visibleEntity in visibleNetworkEntities) _server.SendPacket(visibleEntity.NetPeer, packet);
        });
    }

    #endregion
}