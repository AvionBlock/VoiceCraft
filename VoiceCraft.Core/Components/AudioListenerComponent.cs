using System;
using System.Collections.Generic;
using System.Text;
using Arch.Core;
using Arch.Core.Extensions;
using VoiceCraft.Core.Events;
using VoiceCraft.Core.Network;

namespace VoiceCraft.Core.Components
{
    public class AudioListenerComponent : IAudioInput, ISerializableEntityComponent
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

        public byte[] Serialize()
        {
            var data = new List<byte>();
            data.AddRange(BitConverter.GetBytes(_environmentId.Length));
            if (_environmentId.Length > 0)
                data.AddRange(Encoding.UTF8.GetBytes(_environmentId));

            data.AddRange(BitConverter.GetBytes(_bitmask));

            return data.ToArray();
        }

        public void Deserialize(byte[] data)
        {
            var offset = 0;

            //Extract EnvironmentId
            var environmentIdLength = BitConverter.ToInt32(data, offset);
            offset += sizeof(int);
            if (environmentIdLength > 0)
            {
                _environmentId = Encoding.UTF8.GetString(data);
                offset += environmentIdLength;
            }
            
            //Extract Bitmask
            _bitmask = BitConverter.ToUInt64(data, offset);
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