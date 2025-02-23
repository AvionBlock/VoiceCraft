using System;
using System.Collections.Generic;
using System.Numerics;

namespace VoiceCraft.Core.ECS
{
    public class Entity
    {
        public readonly int Id;
        public readonly World World;
        public readonly Transform Transform;
        public IEnumerable<Component> Components => _components;
        
        private readonly List<Component> _components = new List<Component>();

        public Entity(World world, int id = -1)
        {
            Id = id;
            World = world;
            Transform = new Transform(Vector3.Zero, Quaternion.Identity, Vector3.Zero);
        }

        public bool HasComponent<T>() where T : Component
        {
            return _components.Exists(x => x.GetType() == typeof(T));
        }

        public T GetComponent<T>() where T : Component
        {
            if(!(_components.Find(x => x.GetType() == typeof(T)) is T component)) throw new Exception($"Component {typeof(T)} not found");
            return component;
        }

        public void AddComponent<T>() where T : Component
        {
            if(_components.Exists(x => x.GetType() == typeof(T)))
                throw new InvalidOperationException("Component already exists!");

            var component = (T)Activator.CreateInstance(typeof(T), this);
            if(component != null)
                _components.Add(component);
            throw new InvalidOperationException($"Could not create component of type {typeof(T).Name}");
        }

        public void AddComponent(Component component)
        {
            if(_components.Exists(x => x.GetType() == component.GetType()))
                throw new InvalidOperationException("Component already exists!");
            _components.Add(component);
        }

        public void RemoveComponent<T>() where T : Component
        {
            _components.RemoveAll(x => x.GetType() == typeof(T));
        }
    }
}