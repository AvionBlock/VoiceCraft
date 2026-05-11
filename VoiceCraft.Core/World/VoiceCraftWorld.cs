using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;

namespace VoiceCraft.Core.World
{
    public class VoiceCraftWorld : IDisposable
    {
        private readonly Dictionary<int, VoiceCraftEntity> _entities = new();
        private volatile ImmutableList<VoiceCraftEntity> _entitiesSnapshot =
            ImmutableList<VoiceCraftEntity>.Empty;

        private readonly Lock _lock = new();
        private int _nextEntityId;

        public ImmutableList<VoiceCraftEntity> Entities => _entitiesSnapshot;

        public void Dispose()
        {
            ClearEntities();
            OnEntityCreated = null;
            OnEntityDestroyed = null;
            GC.SuppressFinalize(this);
        }

        public void Reset()
        {
            lock (_lock)
            {
                var entities = _entities.Values.ToArray(); //Copy Array
                foreach (var entity in entities) entity.Reset();
            }
        }

        public event Action<VoiceCraftEntity>? OnEntityCreated;
        public event Action<VoiceCraftEntity>? OnEntityDestroyed;

        public void AddEntity(VoiceCraftEntity entity)
        {
            lock (_lock)
            {
                if (!_entities.TryAdd(entity.Id, entity))
                    throw new InvalidOperationException(
                        "Failed to add entity! An entity with the same id already exists!");
                _entitiesSnapshot = [.._entities.Select(x => x.Value)];

                entity.OnDestroyed += RemoveEntity;
                OnEntityCreated?.Invoke(entity);
            }
        }

        public VoiceCraftEntity? GetEntity(int id)
        {
            lock (_lock)
            {
                _entities.TryGetValue(id, out var entity);
                return entity;
            }
        }

        public bool ContainsEntity(int id)
        {
            lock (_lock)
            {
                return _entities.ContainsKey(id);
            }
        }

        public int GetNextId()
        {
            lock (_lock)
            {
                while (_entities.ContainsKey(_nextEntityId))
                    Interlocked.Increment(ref _nextEntityId);
                return _nextEntityId;
            }
        }

        public void DestroyEntity(int id)
        {
            lock (_lock)
            {
                if (!_entities.Remove(id, out var entity))
                    throw new InvalidOperationException("Failed to destroy entity! Entity not found!");
                _entitiesSnapshot = [.._entities.Select(x => x.Value)];

                entity.OnDestroyed -= RemoveEntity; //No need to listen anymore.
                entity.Destroy();
                OnEntityDestroyed?.Invoke(entity);
            }
        }

        public void ClearEntities()
        {
            lock (_lock)
            {
                //Copy Array.
                var entities = _entities.ToArray();
                _entities.Clear();
                _entitiesSnapshot = ImmutableList<VoiceCraftEntity>.Empty;
                _nextEntityId = 0;

                foreach (var entity in entities)
                {
                    entity.Value.OnDestroyed -= RemoveEntity; //Don't trigger the events!
                    entity.Value.Destroy();
                    OnEntityDestroyed?.Invoke(entity.Value);
                }
            }
        }

        private void RemoveEntity(VoiceCraftEntity entity)
        {
            lock (_lock)
            {
                entity.OnDestroyed -= RemoveEntity;
                if (_entities.Remove(entity.Id, out _))
                    OnEntityDestroyed?.Invoke(entity);
            }
        }
    }
}