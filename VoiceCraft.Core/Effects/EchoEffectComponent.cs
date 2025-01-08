using System;
using Arch.Core;
using VoiceCraft.Core.Interfaces;

namespace VoiceCraft.Core.Effects
{
    public class EchoEffectComponent : IAudioEffect, IComponent
    {
        private float _factor;
        
        public event Action<IComponent>? OnUpdate;
        public event Action<IComponent>? OnDestroy;
        public Guid Id { get; } = Guid.NewGuid();
        public World World { get; }
        public Entity Entity { get; }
        public EffectBitmask Bitmask => EffectBitmask.EchoEffect;

        public float Factor
        {
            get => _factor;
            set
            {
                if (Math.Abs(_factor - value) < 0.0001f) return; //Precision
                _factor = Math.Clamp(value, 0.0f, 1.0f);
                OnUpdate?.Invoke(this);
            }
        }

        public EchoEffectComponent(World world, Entity entity)
        {
            World = world;
            Entity = entity;
        }
        
        public bool IsVisibleToEntity(Entity otherEntity) => true; //Don't care

        public void Process(byte[] buffer)
        {
            throw new NotSupportedException();
        }
        
        public void Destroy()
        {
            OnUpdate = null;
            OnDestroy?.Invoke(this);
            OnDestroy = null;
        }
    }
}