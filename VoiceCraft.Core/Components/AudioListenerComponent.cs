using System;
using Arch.Bus;
using Arch.Core;
using Arch.Core.Extensions;
using LiteNetLib.Utils;
using VoiceCraft.Core.Events;
using VoiceCraft.Core.Network;

namespace VoiceCraft.Core.Components
{
    public class AudioListenerComponent : IAudioInput, INetSerializable, IEntityComponent
    {
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
            _environmentId = reader.GetString();
            _bitmask = reader.GetULong();
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