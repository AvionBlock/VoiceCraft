using System;
using System.Collections.Generic;
using Arch.Core;
using Arch.Core.Extensions;
using VoiceCraft.Core.Events;
using VoiceCraft.Core.Network;

namespace VoiceCraft.Core.Components
{
    public class DirectionalEffectComponent : IAudioEffect, ISerializableEntityComponent, IVisibilityComponent
    {
        private ulong _bitmask; //Will change to default value later.
        private uint _xRotation;
        private uint _yRotation;
        private uint _xRange;
        private uint _yRange;
        private bool _isDisposed;
        private bool IsAlive => !_isDisposed && Entity.IsAlive();
        
        public ComponentType ComponentType => ComponentType.DirectionalEffect;
        
        public Entity Entity { get; }
        
        public event Action? OnDestroyed;
        
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

        public uint XRotation
        {
            get => _xRotation;
            set
            {
                if (_xRotation == value || !IsAlive) return;
                _xRotation = value;
                WorldEventHandler.InvokeComponentUpdated(new ComponentUpdatedEvent(this));
            }
        }

        public uint YRotation
        {
            get => _yRotation;
            set
            {
                if (_yRotation == value || !IsAlive) return;
                _yRotation = value;
                WorldEventHandler.InvokeComponentUpdated(new ComponentUpdatedEvent(this));
            }
        }

        public uint XRange
        {
            get => _xRange;
            set
            {
                if (_xRange == value || !IsAlive) return;
                _xRange = value;
                WorldEventHandler.InvokeComponentUpdated(new ComponentUpdatedEvent(this));
            }
        }

        public uint YRange
        {
            get => _yRange;
            set
            {
                if (_yRange == value || !IsAlive) return;
                _yRange = value;
                WorldEventHandler.InvokeComponentUpdated(new ComponentUpdatedEvent(this));
            }
        }
        
        public DirectionalEffectComponent(Entity entity)
        {
            if (entity.Has<DirectionalEffectComponent>())
                throw new InvalidOperationException($"Entity already has the {GetType().Name}!");
            Entity = entity;
            Entity.Add(this);
            WorldEventHandler.InvokeComponentAdded(new ComponentAddedEvent(this));
        }
        
        public byte[] Serialize()
        {
            var data = new List<byte>();
            data.AddRange(BitConverter.GetBytes(_bitmask));
            data.AddRange(BitConverter.GetBytes(_xRotation));
            data.AddRange(BitConverter.GetBytes(_yRotation));
            data.AddRange(BitConverter.GetBytes(_xRange));
            data.AddRange(BitConverter.GetBytes(_yRange));

            return data.ToArray();
        }

        public void Deserialize(byte[] data)
        {
            var offset = 0;
            
            //Extract Bitmask
            _bitmask = BitConverter.ToUInt64(data, offset);
            offset += sizeof(ulong);
            //Extract xRotation
            _xRotation = BitConverter.ToUInt32(data, offset);
            offset += sizeof(uint);
            //Extract yRotation
            _yRotation = BitConverter.ToUInt32(data, offset);
            offset += sizeof(uint);
            //Extract xRange
            _xRange = BitConverter.ToUInt32(data, offset);
            offset += sizeof(uint);
            //Extract yRange
            _yRange = BitConverter.ToUInt32(data, offset);
        }
        
        public void Dispose()
        {
            if (_isDisposed) return;
            Entity.Remove<DirectionalEffectComponent>();
            _isDisposed = true;
            OnDestroyed?.Invoke();
            WorldEventHandler.InvokeComponentRemoved(new ComponentRemovedEvent(this));
        }

        public bool VisibleTo(Entity entity, ulong bitmask)
        {
            return Entity != entity && IsAlive; //Should not see itself or if the entity/component is dead.
        }
    }
}