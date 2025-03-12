using System;
using System.Collections.Generic;
using System.Numerics;
using Arch.Core;
using Arch.Core.Extensions;
using VoiceCraft.Core.Events;
using VoiceCraft.Core.Network;

namespace VoiceCraft.Core.Components
{
    public class ProximityEffectComponent : IAudioEffect, ISerializableEntityComponent, IVisibilityComponent
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

        public bool VisibleTo(Entity entity, ulong bitmask)
        {
            if (Entity == entity || !IsAlive) return false; //Should not see itself or if the entity/component is dead.
            
            entity.TryGet<ProximityEffectComponent>(out var otherProximityComponent);
            var combinedBitmask = _bitmask | (otherProximityComponent?.Bitmask ?? 0); //Get the combined bitmask of the 2 to compare against.
            var combinedMaxRange = Math.Max(_maxRange, otherProximityComponent?.MaxRange ?? 0); //Get the maximum of the 2.
            if ((combinedBitmask & bitmask) == 0)
                return true; //None of these components are enabled on the bitmask. Entity can be seen.
            
            //Should not see any entities with no transform component.
            if(!entity.Has<TransformComponent>() || !Entity.Has<TransformComponent>()) return false;
            var localtransformComponent = Entity.Get<TransformComponent>();
            var otherTransformComponent = entity.Get<TransformComponent>();
            var distance = Vector3.Distance(localtransformComponent.Position, otherTransformComponent.Position);
            return distance <= combinedMaxRange; //return if the distance of the entities is lower than or equal to the max range of the combined 2 components.
        }
    }
}