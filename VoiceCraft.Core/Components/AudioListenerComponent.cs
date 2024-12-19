using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Arch.Core;
using Arch.Core.Extensions;
using VoiceCraft.Core.Interfaces;

namespace VoiceCraft.Core.Components
{
    public abstract class AudioListenerComponent : IAudioSource
    {
        protected readonly World World;
        protected readonly Entity Entity;
        private readonly List<IAudioEffect> _effects = new List<IAudioEffect>();
        public event Action<AudioListenerComponent, IAudioEffect>? OnEffectAdded;
        public event Action<AudioListenerComponent, IAudioEffect>? OnEffectRemoved;

        public IAudioEffect[] AudioEffects => _effects.ToArray();

        protected AudioListenerComponent(World world, ref Entity entity)
        {
            World = world;
            Entity = entity;
        }

        public virtual bool AddAudioEffect(IAudioEffect audioEffect)
        {
            if (_effects.Exists(e => e.GetType() == audioEffect.GetType())) return false;
            _effects.Add(audioEffect);
            OnEffectAdded?.Invoke(this, audioEffect);
            return true;
        }

        public virtual bool RemoveAudioEffect(int index)
        {
            var audioEffect = _effects.ElementAtOrDefault(index);
            if (audioEffect == null) return false;
            _effects.Remove(audioEffect);
            OnEffectRemoved?.Invoke(this, audioEffect);
            return true;
        }

        public virtual int Read(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        public virtual void GetTrackableEntities(List<Entity> entities)
        {
            if (entities.Contains(Entity)) return;
            entities.Add(Entity);
            var query = new QueryDescription()
                .WithAll<TransformComponent>();

            var currentPosition = Entity.Get<TransformComponent>().Position;
            World.Query(in query, (Entity entity, ref TransformComponent transform) =>
            {
                var components = entity.GetAllComponents();
                foreach (var component in components)
                {
                    if (entities.Contains(entity) || !(component is AudioSourceComponent audioComponent) ||
                        Vector3.Distance(currentPosition, transform.Position) > audioComponent.SourceMaxRange) continue;
                    audioComponent.GetTrackableEntities(entities); //This adds itself.
                    break;
                }
            });
        }
    }
}