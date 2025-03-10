using System;
using Arch.Core;
using Arch.Core.Extensions;
using LiteNetLib.Utils;
using VoiceCraft.Core.Events;

namespace VoiceCraft.Core.Components
{
    public class AudioStreamComponent : IAudioInput, INetSerializable, IEntityComponent
    {
        private bool _isDisposed;
        private bool IsAlive => !_isDisposed && Entity.IsAlive();
        
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
        
        
        public void Serialize(NetDataWriter writer)
        {
            //Do absolutely nothing.
        }

        public void Deserialize(NetDataReader reader)
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