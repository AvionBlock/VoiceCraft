using System;
using System.Collections.Generic;
using System.Numerics;
using Arch.Core;
using Arch.Core.Extensions;
using VoiceCraft.Core.Events;
using VoiceCraft.Core.Network;

namespace VoiceCraft.Core.Components
{
    public class TransformComponent : ISerializableEntityComponent
    {
        private Vector3 _position = Vector3.Zero;
        private Quaternion _rotation = Quaternion.Identity;
        private bool _isDisposed;
        private bool IsAlive => !_isDisposed && Entity.IsAlive();
        
        public ComponentType ComponentType => ComponentType.Transform;
        
        public Entity Entity { get; }
        
        public event Action? OnDestroyed;
        
        public Vector3 Position
        {
            get => _position;
            set
            {
                if(_position == value || !IsAlive) return;
                _position = value;
                WorldEventHandler.InvokeComponentUpdated(new ComponentUpdatedEvent(this));
            }
        }

        public Quaternion Rotation
        {
            get => _rotation;
            set
            {
                if(_rotation == value || !IsAlive) return;
                _rotation = value;
                WorldEventHandler.InvokeComponentUpdated(new ComponentUpdatedEvent(this));
            }
        }

        public TransformComponent(Entity entity)
        {
            if (entity.Has<TransformComponent>())
                throw new InvalidOperationException($"Entity already has the {GetType().Name}!");
            Entity = entity;
            Entity.Add(this);
            WorldEventHandler.InvokeComponentAdded(new ComponentAddedEvent(this));
        }
        
        public byte[] Serialize()
        {
            var data = new List<byte>();
            data.AddRange(BitConverter.GetBytes(Position.X));
            data.AddRange(BitConverter.GetBytes(Position.Y));
            data.AddRange(BitConverter.GetBytes(Position.Z));
            
            data.AddRange(BitConverter.GetBytes(Rotation.X));
            data.AddRange(BitConverter.GetBytes(Rotation.Y));
            data.AddRange(BitConverter.GetBytes(Rotation.Z));
            data.AddRange(BitConverter.GetBytes(Rotation.W));

            return data.ToArray();
        }

        public void Deserialize(byte[] data)
        {
            var offset = 0;
            
            //Extract xPosition
            _position.X = BitConverter.ToSingle(data, offset);
            offset += sizeof(float);
            //Extract yPosition
            _position.Y = BitConverter.ToSingle(data, offset);
            offset += sizeof(float);
            //Extract zPosition
            _position.Z = BitConverter.ToSingle(data, offset);
            offset += sizeof(float);
            
            //Extract xRotation
            _rotation.X = BitConverter.ToSingle(data, offset);
            offset += sizeof(float);
            //Extract yRotation
            _rotation.Y = BitConverter.ToSingle(data, offset);
            offset += sizeof(float);
            //Extract zRotation
            _rotation.Z = BitConverter.ToSingle(data, offset);
            offset += sizeof(float);
            //Extract wRotation
            _rotation.W = BitConverter.ToSingle(data, offset);
        }
        
        public void Dispose()
        {
            if (_isDisposed) return;
            Entity.Remove<TransformComponent>();
            _isDisposed = true;
            OnDestroyed?.Invoke();
            WorldEventHandler.InvokeComponentRemoved(new ComponentRemovedEvent(this));
        }
    }
}