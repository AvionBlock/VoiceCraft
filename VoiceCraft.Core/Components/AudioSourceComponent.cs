using System;
using System.Collections.Generic;
using Arch.Core;
using Arch.Core.Extensions;
using LiteNetLib.Utils;
using VoiceCraft.Core.Events;
using VoiceCraft.Core.Network;

namespace VoiceCraft.Core.Components
{
    public class AudioSourceComponent : IAudioOutput, IVisibleComponent, ISerializableEntityComponent
    {
        public static readonly QueryDescription Query = new QueryDescription().WithAll<AudioSourceComponent>();
        private string _environmentId = string.Empty;
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

        public virtual int ReadOutput(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
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

        public bool VisibleTo(Entity entity)
        {
            entity.TryGet<AudioListenerComponent>(out var audioListenerComponent);
            //Check for audioListener and check against the bitmask and environment ID.
            return (Bitmask & (audioListenerComponent?.Bitmask ?? 0)) != 0 && audioListenerComponent?.EnvironmentId == _environmentId;
        }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(_environmentId);
            writer.Put(_bitmask);
            var componentReference = new ComponentReference(0);

            if (_audioInput is ISerializableEntityComponent entityComponent && entityComponent.Entity.Has<NetworkComponent>())
            {
                var networkComponent = entityComponent.Entity.Get<NetworkComponent>();
                componentReference.NetworkId = networkComponent.NetworkId;
                componentReference.ComponentType = entityComponent.ComponentType;
            }

            writer.Put(componentReference);
        }

        public void Deserialize(NetDataReader reader)
        {
            _environmentId = reader.GetString();
            _bitmask = reader.GetULong();

            var componentReference = new ComponentReference(0);
            componentReference.Deserialize(reader);
            if (componentReference.ComponentType == ComponentType.Unknown) return;

            var world = World.Worlds[Entity.WorldId];
            var component = world.GetComponentFromReference<IAudioInput>(componentReference);
            if (!(component is IAudioInput audioInput)) return;
            _audioInput = audioInput;
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