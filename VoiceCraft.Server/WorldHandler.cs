using Arch.Core;
using VoiceCraft.Core.Components;
using VoiceCraft.Core.Network.Packets;

namespace VoiceCraft.Server
{
    public class WorldHandler
    {
        public World World { get; } = World.Create();

        public delegate void EntityCreated(Entity entity);
        public delegate void EntityDestroyed(Entity entity);
        public delegate void ComponentAdded(Entity entity, object component);
        public delegate void ComponentRemoved(Entity entity, object component);
        
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
        
        public bool Has<T>(Entity entity) => World.Has<T>(entity);

        public void Remove<T>(Entity entity) where T : notnull
        {
            if (!World.Has<T>(entity)) return;
            var removedComponent = World.Get<T>(entity);
            World.Remove<T>(entity);
            OnComponentRemoved?.Invoke(entity, removedComponent);
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