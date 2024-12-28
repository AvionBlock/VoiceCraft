using System;
using Arch.Core;
using VoiceCraft.Core.Interfaces;

namespace VoiceCraft.Core.Effects
{
    public class DirectionalAudioComponent : IAudioEffect, IComponent<DirectionalAudioComponent>
    {
        public event Action<DirectionalAudioComponent>? OnUpdate;
        public event Action<DirectionalAudioComponent>? OnDestroy;
        public Guid Id { get; } = Guid.NewGuid();
        public World World { get; }
        public Entity Entity { get; }
        public EffectBitmask Bitmask => EffectBitmask.DirectionAudio;

        public DirectionalAudioComponent(World world, Entity entity)
        {
            World = world;
            Entity = entity;
        }
        
        public bool CanSeeEntity(Entity entity)
        {
            return true; //True no matter what. This doesn't affect the range at which entities will be able to hear each other.
        }

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