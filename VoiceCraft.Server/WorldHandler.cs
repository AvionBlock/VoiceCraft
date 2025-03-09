using Arch.Core;

namespace VoiceCraft.Server
{
    public class WorldHandler
    {
        public World World { get; } = World.Create();

        public delegate void EntityCreated(Entity entity);
        public delegate void EntityDestroyed(Entity entity);
        public delegate void ComponentAdded(Entity entity, object component);
        public delegate void ComponentRemoved(Entity entity, Type componentType);
        
        public event EntityCreated? OnEntityCreated;
        public event EntityDestroyed? OnEntityDestroyed;
        public event ComponentAdded? OnComponentAdded;
        public event ComponentRemoved? OnComponentRemoved;

        public Entity Create()
        {
            var entity = this.World.Create();
            OnEntityCreated?.Invoke(entity);
            return entity;
        }

        public void Destroy(Entity entity)
        {
            World.Destroy(entity);
            OnEntityDestroyed?.Invoke(entity);
        }

        public void Add<T>(Entity entity, in T component) where T : notnull
        {
            World.Add(entity, in component);
            OnComponentAdded?.Invoke(entity, component);
        }

        public void Remove<T>(Entity entity) where T : notnull
        {
            World.Remove<T>(entity);
            OnComponentRemoved?.Invoke(entity, typeof(T));
        }

        public T Get<T>(Entity entity) => World.Get<T>(entity);
        
        public bool TryGet<T>(Entity entity, out T? component) => World.TryGet(entity, out component);

        public void Query(in QueryDescription queryDescription, ForEach forEach) => World.Query(queryDescription, forEach);

        public void Trim()
        {
            World.TrimExcess();
        }
    }
}