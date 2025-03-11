using System;
using System.Collections.Generic;
using Arch.Core;
using Arch.Core.Extensions;
using VoiceCraft.Core.Events;
using VoiceCraft.Core.Network;

namespace VoiceCraft.Core.Components
{
    public class ProximityEffectComponent : IAudioEffect, ISerializableEntityComponent
    {
        private ulong _bitmask;
        private uint _minRange;
        private uint _maxRange;
        private bool _isDisposed;
        private bool IsAlive => !_isDisposed && Entity.IsAlive();
        
        public ComponentType ComponentType => ComponentType.ProximityEffect;
        
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

        public uint MinRange
        {
            get => _minRange;
            set
            {
                if (_minRange == value || !IsAlive) return;
                _minRange = value;
                WorldEventHandler.InvokeComponentUpdated(new ComponentUpdatedEvent(this));
            }
        }

        public uint MaxRange
        {
            get => _maxRange;
            set
            {
                if (_maxRange == value || !IsAlive) return;
                _maxRange = value;
                WorldEventHandler.InvokeComponentUpdated(new ComponentUpdatedEvent(this));
            }
        }

        public ProximityEffectComponent(Entity entity)
        {
            if (entity.Has<ProximityEffectComponent>())
                throw new InvalidOperationException($"Entity already has the {GetType().Name}!");
            Entity = entity;
            Entity.Add(this);
            WorldEventHandler.InvokeComponentAdded(new ComponentAddedEvent(this));
        }

        public byte[] Serialize()
        {
            var data = new List<byte>();
            data.AddRange(BitConverter.GetBytes(_bitmask));
            data.AddRange(BitConverter.GetBytes(_minRange));
            data.AddRange(BitConverter.GetBytes(_maxRange));

            return data.ToArray();
        }

        public void Deserialize(byte[] data)
        {
            var offset = 0;
            
            //Extract Bitmask
            _bitmask = BitConverter.ToUInt64(data, offset);
            offset += sizeof(ulong);
            //Extract minRange
            _minRange = BitConverter.ToUInt32(data, offset);
            offset += sizeof(uint);
            //Extract maxRange
            _maxRange = BitConverter.ToUInt32(data, offset);
        }
        
        public void Dispose()
        {
            if (_isDisposed) return;
            Entity.Remove<ProximityEffectComponent>();
            _isDisposed = true;
            OnDestroyed?.Invoke();
            WorldEventHandler.InvokeComponentRemoved(new ComponentRemovedEvent(this));
        }
    }
}