using System;
using Arch.Core;
using VoiceCraft.Core.Interfaces;

namespace VoiceCraft.Core.Effects
{
    public class DirectionalEffectComponent : IAudioEffect, IComponent
    {
        public event Action<IComponent>? OnUpdate;
        public event Action<IComponent>? OnDestroy;
        public Guid Id { get; } = Guid.NewGuid();
        public World World { get; }
        public Entity Entity { get; }
        public EffectBitmask Bitmask => EffectBitmask.DirectionalEffect;

        public DirectionalEffectComponent(World world, Entity entity)
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