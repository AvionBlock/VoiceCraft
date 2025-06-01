using System;
using System.Collections.Generic;
using System.Linq;
using VoiceCraft.Core.Interfaces;

namespace VoiceCraft.Core
{
    public class VoiceCraftWorld : IResettable, IDisposable
    {
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
            var id = GetLowestAvailableId();
            var entity = new VoiceCraftEntity(id, this);
            if (!_entities.TryAdd(id, entity))
                throw new InvalidOperationException("Failed to create entity!");

            entity.OnDestroyed += DestroyEntity;
            OnEntityCreated?.Invoke(entity);
            return entity;
        }

        public void AddEntity(VoiceCraftEntity entity)
        {
            if (!_entities.TryAdd(entity.Id, entity))
                throw new InvalidOperationException("Failed to add entity! An entity with the same id already exists!");
            if (entity.World != this)
                throw new InvalidOperationException("Failed to add entity! The entity is not associated with this world!");

            entity.OnDestroyed += DestroyEntity;
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
            entity.OnDestroyed -= DestroyEntity; //No need to listen anymore.
            entity.Destroy();
            OnEntityDestroyed?.Invoke(entity);
        }

        public void ClearEntities()
        {
            var entities = _entities.ToArray();
            _entities.Clear();
            foreach (var entity in entities)
            {
                entity.Value.OnDestroyed -= DestroyEntity; //Don't trigger the events!
                entity.Value.Destroy();
            }
        }

        private void DestroyEntity(VoiceCraftEntity entity)
        {
            entity.OnDestroyed -= DestroyEntity;
            if (_entities.Remove(entity.Id))
                OnEntityDestroyed?.Invoke(entity);
        }

        private int GetLowestAvailableId()
        {
            for (var i = 0; i < int.MaxValue; i++)
                if (!_entities.ContainsKey(i))
                    return i;

            throw new InvalidOperationException("Could not find an available id!");
        }
    }
}