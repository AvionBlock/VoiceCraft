using Arch.Core;

namespace VoiceCraft.Server
{
    public class WorldHandler
    {
        private readonly World _world;
        
        public delegate void EntityCreated(Entity entity);
        public delegate void EntityDestroyed(Entity entity);
        public delegate void ComponentAdded(Entity entity, object component);
        public delegate void ComponentRemoved(Entity entity, Type componentType);
        
        public event EntityCreated? OnEntityCreated;
        public event EntityDestroyed? OnEntityDestroyed;
        public event ComponentAdded? OnComponentAdded;
        public event ComponentRemoved? OnComponentRemoved;

        public WorldHandler()
        {
            _world = World.Create();
        }

        public Entity Create()
        {
            var entity = _world.Create();
            OnEntityCreated?.Invoke(entity);
            return entity;
        }

        public void Destroy(Entity entity)
        {
            _world.Destroy(entity);
            OnEntityDestroyed?.Invoke(entity);
        }

        public void Add<T>(Entity entity, in T component) where T : notnull
        {
            _world.Add(entity, in component);
            OnComponentAdded?.Invoke(entity, component);
        }

        public void Remove<T>(Entity entity) where T : notnull
        {
            _world.Remove<T>(entity);
            OnComponentRemoved?.Invoke(entity, typeof(T));
        }

        public T Get<T>(Entity entity) => _world.Get<T>(entity);

        public void Query(in QueryDescription queryDescription, ForEach forEach) => _world.Query(queryDescription, forEach);
    }
}