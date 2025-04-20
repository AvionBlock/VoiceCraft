using System;
using System.Collections.Generic;

namespace VoiceCraft.Core
{
    public class VoiceCraftWorld : IDisposable //Make this disposable BECAUSE WHY THE FUCK NOT?!
    {
        public event Action<VoiceCraftEntity>? OnEntityCreated;
        public event Action<VoiceCraftEntity>? OnEntityDestroyed;
        public IEnumerable<VoiceCraftEntity> Entities => _entities.Values;

        private readonly Dictionary<byte, VoiceCraftEntity> _entities = new Dictionary<byte, VoiceCraftEntity>();

        public VoiceCraftEntity CreateEntity()
        {
            var id = GetLowestAvailableId();
            var entity = new VoiceCraftEntity(id);
            if (!_entities.TryAdd(id, entity))
                throw new InvalidOperationException("Failed to create entity!");
            
            entity.OnDestroyed += DestroyEntity;
            OnEntityCreated?.Invoke(entity);
            return entity;
        }

        public bool AddEntity(VoiceCraftEntity entity)
        {
            if (!_entities.TryAdd(entity.Id, entity))
                return false;
            
            entity.OnDestroyed += DestroyEntity;
            OnEntityCreated?.Invoke(entity);
            return true;
        }

        public VoiceCraftEntity? GetEntity(byte id)
        {
            _entities.TryGetValue(id, out var entity);
            return entity;
        }

        public bool DestroyEntity(byte id)
        {
            if (!_entities.Remove(id, out var entity)) return false;
            entity.Destroy();
            OnEntityDestroyed?.Invoke(entity);
            return true;
        }

        public void Dispose()
        {
            foreach (var entity in Entities)
            {
                entity.OnDestroyed -= DestroyEntity; //Don't trigger the events!
                entity.Destroy();
            }
            
            _entities.Clear();

            //Deregister all events.
            OnEntityCreated = null;
            OnEntityDestroyed = null;
        }

        private void DestroyEntity(VoiceCraftEntity entity)
        {
            entity.OnDestroyed -= DestroyEntity;
            DestroyEntity(entity.Id);
        }
        
        private byte GetLowestAvailableId()
        {
            for(var i = byte.MinValue; i < byte.MaxValue; ++i)
            {
                if(!_entities.ContainsKey(i)) return i;
            }

            throw new InvalidOperationException("Could not find an available id!");
        }
    }
}