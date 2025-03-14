using System;
using Arch.Core;
using Arch.Core.Extensions;
using LiteNetLib.Utils;
using VoiceCraft.Core.Events;
using VoiceCraft.Core.Network;

namespace VoiceCraft.Core.Components
{
    public class MicrophoneComponent : IAudioStreamable, ISerializableEntityComponent
    {
        public static readonly QueryDescription Query = new QueryDescription().WithAll<MicrophoneComponent>();
        private bool _isDisposed;
        public ComponentType ComponentType => ComponentType.Microphone;
        
        public Entity Entity { get; }

        public event Action? OnDestroyed;
        
        public MicrophoneComponent(Entity entity)
        {
            if (entity.Has<MicrophoneComponent>())
                throw new InvalidOperationException($"Entity already has the {GetType().Name}!");
            if(!entity.TryGet<NetworkComponent>(out var networkComponent) || networkComponent?.NetPeer == null)
                throw new InvalidOperationException($"Component cannot be applied to a server entity!");
            
            Entity = entity;
            Entity.Add(this);
            WorldEventHandler.InvokeComponentAdded(new ComponentAddedEvent(this));
        }
        
        public virtual int ReadStream(byte[] buffer, int offset, int count)
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
            Entity.Remove<MicrophoneComponent>();
            _isDisposed = true;
            OnDestroyed?.Invoke();
            WorldEventHandler.InvokeComponentRemoved(new ComponentRemovedEvent(this));
        }
    }
}