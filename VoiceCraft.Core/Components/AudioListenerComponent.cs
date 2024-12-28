using System;
using System.Collections.Generic;
using System.Linq;
using Arch.Core;
using Arch.Core.Extensions;
using VoiceCraft.Core.Interfaces;

namespace VoiceCraft.Core.Components
{
    public class AudioListenerComponent : IAudioInput, IComponent<AudioListenerComponent>
    {
        public event Action<AudioListenerComponent>? OnUpdate;
        public event Action<AudioListenerComponent>? OnDestroy;
        public Guid Id { get; } = Guid.NewGuid();
        public World World { get; }
        public Entity Entity { get; }

        public AudioListenerComponent(World world, Entity entity)
        {
            World = world;
            Entity = entity;
        }
        
        public int Read(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        public void GetVisibleEntities(List<Entity> entities)
        {
            if (entities.Contains(Entity)) return;
            var query = new QueryDescription()
                .WithAll<TransformComponent>();
            var selfEffects = Entity.GetAllComponents().OfType<IAudioEffect>();
            World.Query(in query, (Entity entity, ref TransformComponent transform) =>
            {
                var entityComponents = entity.GetAllComponents();
                
                //Loop through all the audio effects first.
                if (selfEffects.Any(effectComponent => !effectComponent.CanSeeEntity(entity)))
                    return; //Cannot see entity, return.
                
                //Can see entity, Get audio outputs and get the visible entities of that audio output.
                foreach (var sourceComponent in entityComponents)
                    if (sourceComponent is IAudioOutput audioOutput)
                        audioOutput.GetVisibleEntities(entities);
            });

        }
        public void Destroy()
        {
            OnUpdate = null;
            OnDestroy?.Invoke(this);
            OnDestroy = null;
        }
    }
}