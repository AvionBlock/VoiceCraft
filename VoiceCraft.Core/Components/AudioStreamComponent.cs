using System;
using Arch.Core;
using Arch.Core.Extensions;
using LiteNetLib.Utils;
using VoiceCraft.Core.Events;
using VoiceCraft.Core.Network;

namespace VoiceCraft.Core.Components
{
    public class AudioStreamComponent : IAudioInput, ISerializableEntityComponent
    {
        public static readonly QueryDescription Query = new QueryDescription().WithAll<AudioStreamComponent>();
        private IAudioStreamable? _audioStreamable;
        private bool _isDisposed;
        private bool IsAlive => !_isDisposed && Entity.IsAlive();

        public ComponentType ComponentType => ComponentType.AudioStream;

        public Entity Entity { get; }

        public event Action? OnDestroyed;

        public IAudioStreamable? AudioStream
        {
            get => _audioStreamable;
            set
            {
                if (_audioStreamable == value || !IsAlive) return;
                _audioStreamable = value;
                WorldEventHandler.InvokeComponentUpdated(new ComponentUpdatedEvent(this));
            }
        }

        public AudioStreamComponent(Entity entity)
        {
            if (entity.Has<AudioStreamComponent>())
                throw new InvalidOperationException($"Entity already has the {GetType().Name}!");
            Entity = entity;
            Entity.Add(this);
            WorldEventHandler.InvokeComponentAdded(new ComponentAddedEvent(this));
        }

        public virtual int ReadInput(byte[] buffer, int offset, int count)
        {
            return _audioStreamable?.ReadStream(buffer, offset, count) ?? 0;
        }

        public void Serialize(NetDataWriter writer)
        {
            var componentReference = new ComponentReference(0);

            if (_audioStreamable is ISerializableEntityComponent entityComponent && entityComponent.Entity.Has<NetworkComponent>())
            {
                var networkComponent = entityComponent.Entity.Get<NetworkComponent>();
                componentReference.NetworkId = networkComponent.NetworkId;
                componentReference.ComponentType = entityComponent.ComponentType;
            }

            writer.Put(componentReference);
        }

        public void Deserialize(NetDataReader reader)
        {
            var componentReference = new ComponentReference(0);
            componentReference.Deserialize(reader);
            if (componentReference.ComponentType == ComponentType.Unknown) return;

            var world = World.Worlds[Entity.WorldId];
            var component = world.GetComponentFromReference<IAudioStreamable>(componentReference);
            if (!(component is IAudioStreamable audioStreamable)) return;
            _audioStreamable = audioStreamable;
        }

        public void Dispose()
        {
            if (_isDisposed) return;
            Entity.Remove<AudioStreamComponent>();
            _isDisposed = true;
            OnDestroyed?.Invoke();
            WorldEventHandler.InvokeComponentRemoved(new ComponentRemovedEvent(this));
        }
    }
}