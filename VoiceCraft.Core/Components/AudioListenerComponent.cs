using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Arch.Core;
using Arch.Core.Extensions;
using LiteNetLib.Utils;
using VoiceCraft.Core.Events;
using VoiceCraft.Core.Network;

namespace VoiceCraft.Core.Components
{
    public class AudioListenerComponent : IAudioInput, ISerializableEntityComponent, IVisibleComponent
    {
        private readonly QueryDescription _query = new QueryDescription().WithAll<AudioSourceComponent>();
        private string _environmentId = string.Empty;
        private ulong _bitmask; //Will change to a default value later.
        private bool _isDisposed;
        private bool IsAlive => !_isDisposed && Entity.IsAlive();

        public ComponentType ComponentType => ComponentType.AudioListener;
        public Entity Entity { get; }

        public event Action? OnDestroyed;

        public string EnvironmentId
        {
            get => _environmentId;
            set
            {
                if (_environmentId == value || !IsAlive) return;
                _environmentId = value;
                WorldEventHandler.InvokeComponentUpdated(new ComponentUpdatedEvent(this));
            }
        }

        public ulong Bitmask
        {
            get => _bitmask;
            set
            {
                if (_bitmask == value || !IsAlive) return;
                _bitmask = value;
                WorldEventHandler.InvokeComponentUpdated(new ComponentUpdatedEvent(this));
            }
        }

        public AudioListenerComponent(Entity entity)
        {
            if (entity.Has<AudioListenerComponent>())
                throw new InvalidOperationException($"Entity already has the {GetType().Name}!");
            Entity = entity;
            Entity.Add(this);
            WorldEventHandler.InvokeComponentAdded(new ComponentAddedEvent(this));
        }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(EnvironmentId);
            writer.Put(Bitmask);
        }

        public void Deserialize(NetDataReader reader)
        {
            EnvironmentId = reader.GetString();
            Bitmask = reader.GetULong();
        }

        public bool VisibleTo(Entity entity)
        {
            entity.TryGet<AudioSourceComponent>(out var audioSourceComponent);
            return (Bitmask & (audioSourceComponent?.Bitmask ?? 0)) != 0; //Check for audioListener and check against the bitmask.
        }

        public void GetVisibleEntities(World world, List<Entity> entities)
        {
            world.Query(in _query, (ref AudioSourceComponent audioListenerComponent) =>
            {
                if (audioListenerComponent.Entity == Entity || entities.Contains(audioListenerComponent.Entity))
                    return; //Already checked entity or it's a local entity.
                
                var components = audioListenerComponent.Entity.GetAllComponents();

                foreach (var component in components)
                {
                    if (!(component is IVisibilityComponent visibilityComponent)) continue;
                    if (!visibilityComponent.VisibleTo(Entity)) return; //Not visible return the function.
                }

                //Visible. Add the entity.
                entities.Add(audioListenerComponent.Entity);
                //Loop through all the components that can get more visible entities.
                foreach (var component in components)
                {
                    if (!(component is IVisibleComponent visibilityComponent)) continue;
                    visibilityComponent.GetVisibleEntities(world, entities); //Get all visible entities from this component on the entity.
                }
            });
        }

        public void Dispose()
        {
            if (_isDisposed) return;
            Entity.Remove<AudioListenerComponent>();
            _isDisposed = true;
            OnDestroyed?.Invoke();
            WorldEventHandler.InvokeComponentRemoved(new ComponentRemovedEvent(this));
        }
    }
}