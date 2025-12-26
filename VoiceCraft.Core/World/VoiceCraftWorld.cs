using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using LiteNetLib;
using VoiceCraft.Core.Interfaces;

namespace VoiceCraft.Core.World
{
    public class VoiceCraftWorld : IResettable, IDisposable
    {
        private int _nextEntityId;
        private readonly Mutex _mutex = new Mutex();

        private readonly ConcurrentDictionary<int, VoiceCraftEntity> _entities =
            new ConcurrentDictionary<int, VoiceCraftEntity>();

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
            OnEntityDestroyed?.Invoke(entity);
        }

        public void ClearEntities()
        {
            //Copy Array.
            var entities = _entities.ToArray();
            _entities.Clear();
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
            if (_entities.TryRemove(entity.Id, out _))
                OnEntityDestroyed?.Invoke(entity);
        }

        private int GetNextId()
        {
            _mutex.WaitOne();
            try
            {
                while (_entities.ContainsKey(_nextEntityId))
                {
                    ++_nextEntityId;
                }

                return _nextEntityId++;
            }
            finally
            {
                _mutex.ReleaseMutex();
            }
        }
    }
}