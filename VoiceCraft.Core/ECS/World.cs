using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;

namespace VoiceCraft.Core.ECS
{
    public class World
    {
        public IEnumerable<Entity> Entities => _entities.Values;
        
        private readonly ConcurrentDictionary<int, Entity> _entities = new ConcurrentDictionary<int, Entity>();

        public Entity CreateEntity()
        {
            for (var i = 0; i < int.MaxValue; i++)
            {
                if (_entities.ContainsKey(i)) continue;
                var entity = new Entity(this, i);
                _entities.TryAdd(i, entity);
                return entity;
            }

            throw new ConstraintException("Could not create entity as max value for entities has been reached!");
        }

        public Entity CreateEntity(int id)
        {
            if(_entities.ContainsKey(id)) throw new InvalidOperationException($"Entity with id {id} already exists!");
            var entity = new Entity(this, id);
            _entities.TryAdd(id, entity);
            return entity;
        }

        public void DestroyEntity(Entity entity)
        {
            _entities.TryRemove(entity.Id, out _);
        }

        public Entity? DestroyEntity(int id)
        {
            _entities.TryRemove(id, out var entity);
            return entity;
        }
    }
}