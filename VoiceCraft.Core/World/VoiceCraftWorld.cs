using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace VoiceCraft.Core.World
{
    public class VoiceCraftWorld : IDisposable
    {
        private readonly ConcurrentDictionary<int, VoiceCraftEntity> _entities = new();
        private int _nextEntityId;

        public IEnumerable<VoiceCraftEntity> Entities => _entities.Values;

        public void Dispose()
        {
            ClearEntities();
            OnEntityCreated = null;
            OnEntityDestroyed = null;
            GC.SuppressFinalize(this);
        }

        public void Reset()
        {
            var entities = _entities.ToArray();
            foreach (var entity in entities) entity.Value.Reset();
        }

        public event Action<VoiceCraftEntity>? OnEntityCreated;
        public event Action<VoiceCraftEntity>? OnEntityDestroyed;

        public void AddEntity(VoiceCraftEntity entity)
        {
            if (!_entities.TryAdd(entity.Id, entity))
                throw new InvalidOperationException("Failed to add entity! An entity with the same id already exists!");

            entity.OnDestroyed += RemoveEntity;
            OnEntityCreated?.Invoke(entity);
        }

        public VoiceCraftEntity? GetEntity(int id)
        {
            _entities.TryGetValue(id, out var entity);
            return entity;
        }

        public int GetNextId()
        {
            while (_entities.ContainsKey(_nextEntityId))
                Interlocked.Increment(ref _nextEntityId);

            return _nextEntityId;
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
                OnEntityDestroyed?.Invoke(entity.Value);
            }
        }

        private void RemoveEntity(VoiceCraftEntity entity)
        {
            entity.OnDestroyed -= RemoveEntity;
            if (_entities.TryRemove(entity.Id, out _))
                OnEntityDestroyed?.Invoke(entity);
        }
    }
}