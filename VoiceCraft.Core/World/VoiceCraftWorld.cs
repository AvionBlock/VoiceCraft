using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using LiteNetLib;
using VoiceCraft.Core.Interfaces;

namespace VoiceCraft.Core.World
{
    public class VoiceCraftWorld : IResettable, IDisposable
    {
        private int _nextEntityId;
        private readonly ConcurrentQueue<int> _entityIds = new ConcurrentQueue<int>();
        private readonly Dictionary<int, VoiceCraftEntity> _entities = new Dictionary<int, VoiceCraftEntity>();
        public IEnumerable<VoiceCraftEntity> Entities => _entities.Values;

        public void Dispose()
        {
            ClearEntities();
            OnEntityCreated = null;
            OnEntityDestroyed = null;
        }

        public void Reset()
        {
            ClearEntities();
        }

        public event Action<VoiceCraftEntity>? OnEntityCreated;
        public event Action<VoiceCraftEntity>? OnEntityDestroyed;

        public VoiceCraftEntity CreateEntity()
        {
            var id = GetNextId();
            var entity = new VoiceCraftEntity(id, this);
            if (!_entities.TryAdd(id, entity))
                throw new InvalidOperationException("Failed to create entity!");

            entity.OnDestroyed += RemoveEntity;
            OnEntityCreated?.Invoke(entity);
            return entity;
        }

        public VoiceCraftNetworkEntity CreateEntity(NetPeer peer, Guid userGuid, Guid serverUserGuid, string locale,
            PositioningType positioningType)
        {
            var id = GetNextId();
            var entity = new VoiceCraftNetworkEntity(peer, id, userGuid, serverUserGuid, locale, positioningType, this);
            if (!_entities.TryAdd(id, entity))
                throw new InvalidOperationException("Failed to create entity!");

            peer.Tag = entity;
            entity.OnDestroyed += RemoveEntity;
            OnEntityCreated?.Invoke(entity);
            return entity;
        }

        public void AddEntity(VoiceCraftEntity entity)
        {
            if (!_entities.TryAdd(entity.Id, entity))
                throw new InvalidOperationException("Failed to add entity! An entity with the same id already exists!");
            if (entity.World != this)
                throw new InvalidOperationException(
                    "Failed to add entity! The entity is not associated with this world!");

            entity.OnDestroyed += RemoveEntity;
            OnEntityCreated?.Invoke(entity);
        }

        public VoiceCraftEntity? GetEntity(int id)
        {
            _entities.TryGetValue(id, out var entity);
            return entity;
        }

        public void DestroyEntity(int id)
        {
            if (!_entities.Remove(id, out var entity))
                throw new InvalidOperationException("Failed to destroy entity! Entity not found!");
            entity.OnDestroyed -= RemoveEntity; //No need to listen anymore.
            entity.Destroy();
            _entityIds.Enqueue(id);
            OnEntityDestroyed?.Invoke(entity);
        }

        public void ClearEntities()
        {
            //Copy Array.
            var entities = _entities.ToArray();
            _entities.Clear();
            _entityIds.Clear();
            _nextEntityId = 0;
            
            foreach (var entity in entities)
            {
                entity.Value.OnDestroyed -= RemoveEntity; //Don't trigger the events!
                entity.Value.Destroy();
            }
        }

        private void RemoveEntity(VoiceCraftEntity entity)
        {
            entity.OnDestroyed -= RemoveEntity;
            if (_entities.Remove(entity.Id))
                OnEntityDestroyed?.Invoke(entity);
        }

        private int GetNextId()
        {
            return _entityIds.TryDequeue(out var id) ? id : _nextEntityId++;
        }
    }
}