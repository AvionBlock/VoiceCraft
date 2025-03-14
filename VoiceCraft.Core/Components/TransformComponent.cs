using System;
using System.Numerics;
using Arch.Core;
using Arch.Core.Extensions;
using LiteNetLib.Utils;
using VoiceCraft.Core.Events;
using VoiceCraft.Core.Network;

namespace VoiceCraft.Core.Components
{
    public class TransformComponent : ISerializableEntityComponent
    {
        public static readonly QueryDescription Query = new QueryDescription().WithAll<TransformComponent>();
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
        
        public void Serialize(NetDataWriter writer)
        {
            writer.Put(Position.X);
            writer.Put(Position.Y);
            writer.Put(Position.Z);
            
            writer.Put(Rotation.X);
            writer.Put(Rotation.Y);
            writer.Put(Rotation.Z);
            writer.Put(Rotation.W);
        }

        public void Deserialize(NetDataReader reader)
        {
            var x = reader.GetFloat();
            var y = reader.GetFloat();
            var z = reader.GetFloat();
            
            var rX = reader.GetFloat();
            var rY = reader.GetFloat();
            var rZ = reader.GetFloat();
            var rW = reader.GetFloat();
            
            Position = new Vector3(x, y, z);
            Rotation = new Quaternion(rX, rY, rZ, rW);
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