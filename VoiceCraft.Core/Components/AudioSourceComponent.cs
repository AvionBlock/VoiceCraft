using System;
using System.Collections.Generic;
using System.Text;
using Arch.Core;
using Arch.Core.Extensions;
using VoiceCraft.Core.Events;
using VoiceCraft.Core.Network;

namespace VoiceCraft.Core.Components
{
    public class AudioSourceComponent : IAudioOutput, ISerializableEntityComponent
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

        public byte[] Serialize()
        {
            var data = new List<byte>();
            data.AddRange(BitConverter.GetBytes(_environmentId.Length));
            if (_environmentId.Length > 0)
                data.AddRange(Encoding.UTF8.GetBytes(_environmentId));

            data.AddRange(BitConverter.GetBytes(_name.Length));
            if (_name.Length > 0)
                data.AddRange(Encoding.UTF8.GetBytes(_name));

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
            
            //Extract Name
            var nameLength = BitConverter.ToInt32(data, offset);
            offset += sizeof(int);
            if (nameLength > 0)
            {
                _name = Encoding.UTF8.GetString(data);
                offset += nameLength;
            }
            
            //Extract Bitmask
            _bitmask = BitConverter.ToUInt64(data, offset);
        }

        public void GetVisibleComponents(World world, List<object> components)
        {
            if (components.Contains(this) || !IsAlive)
                return; //Already part of the list. don't need to recheck through or if the component/entity is dead. Also prevents stack overflows (I think).
            components.Add(this);
            _audioInput?.GetVisibleComponents(world, components); //Get visible components of the linked audioInput.
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