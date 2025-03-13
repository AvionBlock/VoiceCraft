using System;
using System.Collections.Generic;
using Arch.Core;
using Arch.Core.Extensions;
using LiteNetLib.Utils;
using VoiceCraft.Core.Events;
using VoiceCraft.Core.Network;

namespace VoiceCraft.Core.Components
{
    public class AudioSourceComponent : IAudioOutput, ISerializableEntityComponent, IVisibleComponent
    {
        private string _environmentId = string.Empty;
        private string _name = string.Empty;
        private ulong _bitmask; //will change to a default value later.
        private IAudioInput? _audioInput;
        private bool _isDisposed;
        private bool IsAlive => !_isDisposed && Entity.IsAlive();

        public ComponentType ComponentType => ComponentType.AudioSource;

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

        public string Name
        {
            get => _name;
            set
            {
                if (_name == value || !IsAlive) return;
                _name = value;
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

        public IAudioInput? AudioInput
        {
            get => _audioInput;
            set
            {
                if (_audioInput == value || !IsAlive) return;
                _audioInput = value;
                WorldEventHandler.InvokeComponentUpdated(new ComponentUpdatedEvent(this));
            }
        }

        public AudioSourceComponent(Entity entity)
        {
            if (entity.Has<AudioSourceComponent>())
                throw new InvalidOperationException($"Entity already has the {GetType().Name}!");
            Entity = entity;
            Entity.Add(this);
            WorldEventHandler.InvokeComponentAdded(new ComponentAddedEvent(this));
        }
        
        public void Serialize(NetDataWriter writer)
        {
            writer.Put(_environmentId);
            writer.Put(_name);
            writer.Put(_bitmask);
        }

        public void Deserialize(NetDataReader reader)
        {
            _environmentId = reader.GetString();
            _name = reader.GetString();
            _bitmask = reader.GetULong();
            
            //TODO Figure out component references later.
        }
        
        public bool VisibleTo(Entity entity)
        {
            entity.TryGet<AudioListenerComponent>(out var audioListenerComponent);
            return (Bitmask & (audioListenerComponent?.Bitmask ?? 0)) != 0; //Check for audioListener and check against the bitmask.
        }

        public void GetVisibleEntities(World world, List<Entity> entities)
        {
            if (!(_audioInput is IEntityComponent audioEntityComponent) || entities.Contains(audioEntityComponent.Entity)) return;
            entities.Add(audioEntityComponent.Entity); //No matter what it's visible because of the direct reference.
            var components = audioEntityComponent.Entity.GetAllComponents();
            
            //Get all visible entities from the entity.
            foreach (var component in components)
            {
                if (!(component is IVisibleComponent visibilityComponent)) continue;
                visibilityComponent.GetVisibleEntities(world, entities);
            }
        }

        public void Dispose()
        {
            if (_isDisposed) return;
            Entity.Remove<AudioSourceComponent>();
            _isDisposed = true;
            OnDestroyed?.Invoke();
            WorldEventHandler.InvokeComponentRemoved(new ComponentRemovedEvent(this));
        }
    }
}