using System;
using Arch.Bus;
using Arch.Core;
using Arch.Core.Extensions;
using LiteNetLib.Utils;
using VoiceCraft.Core.Events;
using VoiceCraft.Core.Network;

namespace VoiceCraft.Core.Components
{
    public class ProximityEffectComponent : IAudioEffect, INetSerializable, IEntityComponent
    {
        private uint _bitmask;
        private uint _minRange;
        private uint _maxRange;
        private bool _isDisposed;
        private bool IsAlive => !_isDisposed && Entity.IsAlive();
        
        public ComponentType ComponentType => ComponentType.ProximityEffect;
        
        public Entity Entity { get; }
        
        public event Action? OnDestroyed;

        public uint Bitmask
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

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(_bitmask);
            writer.Put(_minRange);
            writer.Put(_maxRange);
        }

        public void Deserialize(NetDataReader reader)
        {
            _bitmask = reader.GetUInt();
            _minRange = reader.GetUInt();
            _maxRange = reader.GetUInt();
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