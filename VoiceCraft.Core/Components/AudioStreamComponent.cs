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
        private bool _isDisposed;
        private bool IsAlive => !_isDisposed && Entity.IsAlive();
        
        public ComponentType ComponentType => ComponentType.AudioStream;
        
        public Entity Entity { get; }
        
        public event Action? OnDestroyed;
        
        public AudioStreamComponent(Entity entity)
        {
            if (entity.Has<AudioStreamComponent>())
                throw new InvalidOperationException($"Entity already has the {GetType().Name}!");
            Entity = entity;
            Entity.Add(this);
            WorldEventHandler.InvokeComponentAdded(new ComponentAddedEvent(this));
        }
        
        
        public byte[]? Serialize()
        {
            //Do absolutely nothing.
            return null;
        }

        public void Deserialize(byte[] data)
        {
            //Do absolutely nothing.
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