using System;
using Arch.Bus;
using Arch.Core;
using Arch.Core.Extensions;
using LiteNetLib.Utils;
using VoiceCraft.Core.Events;
using VoiceCraft.Core.Network;

namespace VoiceCraft.Core.Components
{
    public class DirectionalEffectComponent : IAudioEffect, INetSerializable, IEntityComponent
    {
        private uint _bitmask; //Will change to default value later.
        private uint _xRotation;
        private uint _yRotation;
        private uint _xRange;
        private uint _yRange;
        private bool _isDisposed;
        private bool IsAlive => !_isDisposed && Entity.IsAlive();
        
        public ComponentType ComponentType => ComponentType.DirectionalEffect;
        
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

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(_bitmask);
            writer.Put(_xRotation);
            writer.Put(_yRotation);
            writer.Put(_xRange);
            writer.Put(_yRange);
        }

        public void Deserialize(NetDataReader reader)
        {
            _bitmask = reader.GetUInt();
            _xRotation = reader.GetUInt();
            _yRotation = reader.GetUInt();
            _xRange = reader.GetUInt();
            _yRange = reader.GetUInt();
        }
        
        public void Dispose()
        {
            if (_isDisposed) return;
            Entity.Remove<DirectionalEffectComponent>();
            _isDisposed = true;
            OnDestroyed?.Invoke();
            WorldEventHandler.InvokeComponentRemoved(new ComponentRemovedEvent(this));
        }
    }
}