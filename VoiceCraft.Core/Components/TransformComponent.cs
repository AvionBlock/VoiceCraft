using System;
using System.Numerics;
using Arch.Core;
using VoiceCraft.Core.Interfaces;

namespace VoiceCraft.Core.Components
{
    public class TransformComponent : IComponent
    {
        private Vector3 _position = Vector3.Zero;
        private Quaternion _rotation = Quaternion.Identity;
        private Vector3 _scale = Vector3.One;
        
        public event Action<IComponent>? OnUpdate;
        public event Action<IComponent>? OnDestroy;
        public Guid Id { get; } = Guid.NewGuid();
        public World World { get; }
        public Entity Entity { get; }
        public Vector3 Position
        {
            get => _position;
            set
            {
                if (value == _position) return;
                _position = value;
                OnUpdate?.Invoke(this);
            }
        }

        public Quaternion Rotation
        {
            get => _rotation;
            set
            {
                if (value == _rotation) return;
                _rotation = value;
                OnUpdate?.Invoke(this);
            }
        }

        public Vector3 Scale
        {
            get => _scale;
            set
            {
                if (value == _scale) return;
                _scale = value;
                OnUpdate?.Invoke(this);
            }
        }

        public TransformComponent(World world, Entity entity)
        {
            World = world;
            Entity = entity;
        }
        
        public bool IsVisibleToEntity(Entity otherEntity) => true; //Don't care
        
        public void Destroy()
        {
            OnUpdate = null;
            OnDestroy?.Invoke(this);
            OnDestroy = null;
        }
    }
}