using System;
using System.Collections.Generic;
using System.Linq;
using Arch.Core;
using Arch.Core.Extensions;
using VoiceCraft.Core.Interfaces;

namespace VoiceCraft.Core.Components
{
    public class AudioListenerComponent : IAudioInput, IComponent
    {
        public event Action<IComponent>? OnUpdate;
        public event Action<IComponent>? OnDestroy;
        public Guid Id { get; } = Guid.NewGuid();
        public World World { get; }
        public Entity Entity { get; }

        public AudioListenerComponent(World world, Entity entity)
        {
            World = world;
            Entity = entity;
        }
        
        public bool IsVisibleToEntity(Entity otherEntity) => true; //Don't care
        
        public int Read(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        public void GetVisibleEntities(List<Entity> entities)
        {
            if (entities.Contains(Entity)) return;
            var query = new QueryDescription()
                .WithAll<TransformComponent>();
            World.Query(in query, (Entity otherEntity, ref TransformComponent transform) =>
            {
                var otherEntityComponents = otherEntity.GetAllComponents().OfType<IComponent>().ToList();
                if (!otherEntityComponents.Exists(x => x is IAudioOutput)) return;
                
                var entityComponents = Entity.GetAllComponents().OfType<IComponent>().ToList();
                var componentTypes = new List<Type>();
                foreach (var component in entityComponents)
                {
                    component.IsVisibleToEntity(otherEntity);
                    componentTypes.Add(component.GetType());
                }

                foreach (var component in otherEntityComponents.Where(component => !componentTypes.Contains(component.GetType())))
                {
                    component.IsVisibleToEntity(Entity);
                }
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