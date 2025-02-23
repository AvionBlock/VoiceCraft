using System;
using System.Collections.Generic;

namespace VoiceCraft.Core.ECS
{
    public class Entity
    {
        public readonly int Id;
        public readonly World World;
        public IEnumerable<IComponent> Components => _components;
        
        private readonly List<IComponent> _components = new List<IComponent>();

        public Entity(World world, int id = -1)
        {
            Id = id;
            World = world;
        }

        public void AddComponent<T>() where T : IComponent
        {
            if(_components.Exists(x => x.GetType() == typeof(T)))
                throw new InvalidOperationException("Component already exists!");
            
            var component = Activator.CreateInstance<T>();
            if(component != null)
                _components.Add(component);
            throw new InvalidOperationException($"Could not create component of type {typeof(T).Name}");
        }

        public void RemoveComponent<T>() where T : IComponent
        {
            _components.RemoveAll(x => x.GetType() == typeof(T));
        }
    }
}