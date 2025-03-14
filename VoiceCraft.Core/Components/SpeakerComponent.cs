using System;
using Arch.Core;
using Arch.Core.Extensions;
using LiteNetLib.Utils;
using VoiceCraft.Core.Events;
using VoiceCraft.Core.Network;

namespace VoiceCraft.Core.Components
{
    public class SpeakerComponent : ISerializableEntityComponent
    {
        public static readonly QueryDescription Query = new QueryDescription().WithAll<SpeakerComponent>();
        private IAudioInput? _audioInput;
        private bool _isDisposed;
        private bool IsAlive => !_isDisposed && Entity.IsAlive();
        
        public ComponentType ComponentType => ComponentType.Microphone;
        
        public Entity Entity { get; }

        public event Action? OnDestroyed;
        
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
        
        public SpeakerComponent(Entity entity)
        {
            if (entity.Has<MicrophoneComponent>())
                throw new InvalidOperationException($"Entity already has the {GetType().Name}!");
            if(!entity.TryGet<NetworkComponent>(out var networkComponent) || networkComponent?.NetPeer == null)
                throw new InvalidOperationException($"Component cannot be applied to a server entity!");
            
            Entity = entity;
            Entity.Add(this);
            WorldEventHandler.InvokeComponentAdded(new ComponentAddedEvent(this));
        }
        
        public virtual int ReadInput(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }
        
        public void Serialize(NetDataWriter writer)
        {
            //Do nothing
        }

        public void Deserialize(NetDataReader reader)
        {
            //Do nothing
        }

        public void Dispose()
        {
            if (_isDisposed) return;
            Entity.Remove<SpeakerComponent>();
            _isDisposed = true;
            OnDestroyed?.Invoke();
            WorldEventHandler.InvokeComponentRemoved(new ComponentRemovedEvent(this));
        }
    }
}