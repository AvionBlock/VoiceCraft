using Arch.Bus;
using Arch.Core;
using LiteNetLib.Utils;
using VoiceCraft.Core.Events;

namespace VoiceCraft.Core.Components
{
    public class DirectionalEffectComponent : IAudioEffect, IComponentSerializable
    {
        private uint _bitmask; //Will change to default value later.
        private uint _xRotation;
        private uint _yRotation;
        private uint _xRange;
        private uint _yRange;

        public World World { get; }
        public Entity Entity { get; }
        
        public uint Bitmask
        {
            get => _bitmask;
            set
            {
                if (_bitmask == value) return;
                _bitmask = value;
                var componentUpdated = new ComponentUpdatedEvent(this);
                EventBus.Send(ref componentUpdated);
            }
        }

        public uint XRotation
        {
            get => _xRotation;
            set
            {
                if (_xRotation == value) return;
                _xRotation = value;
                var componentUpdated = new ComponentUpdatedEvent(this);
                EventBus.Send(ref componentUpdated);
            }
        }

        public uint YRotation
        {
            get => _yRotation;
            set
            {
                if (_yRotation == value) return;
                _yRotation = value;
                var componentUpdated = new ComponentUpdatedEvent(this);
                EventBus.Send(ref componentUpdated);
            }
        }

        public uint XRange
        {
            get => _xRange;
            set
            {
                if (_xRange == value) return;
                _xRange = value;
                var componentUpdated = new ComponentUpdatedEvent(this);
                EventBus.Send(ref componentUpdated);
            }
        }

        public uint YRange
        {
            get => _yRange;
            set
            {
                if (_yRange == value) return;
                _yRange = value;
                var componentUpdated = new ComponentUpdatedEvent(this);
                EventBus.Send(ref componentUpdated);
            }
        }

        public DirectionalEffectComponent(World world, Entity entity)
        {
            World = world;
            Entity = entity;
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
    }
}