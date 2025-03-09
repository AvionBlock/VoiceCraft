using System.Numerics;
using Arch.Bus;
using Arch.Core;
using LiteNetLib.Utils;
using VoiceCraft.Core.Events;

namespace VoiceCraft.Core.Components
{
    public class TransformComponent : IComponentSerializable
    {
        private Vector3 _position = Vector3.Zero;
        private Quaternion _rotation = Quaternion.Identity;

        public World World { get; }
        public Entity Entity { get; }
        
        public Vector3 Position
        {
            get => _position;
            set
            {
                if(_position == value) return;
                _position = value;
                var componentUpdated = new ComponentUpdatedEvent(this);
                EventBus.Send(ref componentUpdated);
            }
        }

        public Quaternion Rotation
        {
            get => _rotation;
            set
            {
                if(_rotation == value) return;
                _rotation = value;
                var componentUpdated = new ComponentUpdatedEvent(this);
                EventBus.Send(ref componentUpdated);
            }
        }

        public TransformComponent(World world, Entity entity)
        {
            World = world;
            Entity = entity;
        }

        public void Serialize(NetDataWriter writer)
        {
            //Position
            writer.Put(Position.X);
            writer.Put(Position.Y);
            writer.Put(Position.Z);
            //Rotation
            writer.Put(Rotation.X);
            writer.Put(Rotation.Y);
            writer.Put(Rotation.Z);
            writer.Put(Rotation.W);
        }

        public void Deserialize(NetDataReader reader)
        {
            //Position
            _position.X = reader.GetFloat();
            _position.Y = reader.GetFloat();
            _position.Z = reader.GetFloat();
            //Rotation
            _rotation.X = reader.GetFloat();
            _rotation.Y = reader.GetFloat();
            _rotation.Z = reader.GetFloat();
            _rotation.W = reader.GetFloat();
        }
    }
}