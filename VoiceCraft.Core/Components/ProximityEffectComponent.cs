using Arch.Bus;
using Arch.Core;
using LiteNetLib.Utils;
using VoiceCraft.Core.Events;

namespace VoiceCraft.Core.Components
{
    public class ProximityEffectComponent : IAudioEffect, IComponentSerializable
    {
        private uint _bitmask;
        private uint _minRange;
        private uint _maxRange;
        
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

        public uint MinRange
        {
            get => _minRange;
            set
            {
                if (_minRange == value) return;
                _minRange = value;
                var componentUpdated = new ComponentUpdatedEvent(this);
                EventBus.Send(ref componentUpdated);
            }
        }

        public uint MaxRange
        {
            get => _maxRange;
            set
            {
                if (_maxRange == value) return;
                _maxRange = value;
                var componentUpdated = new ComponentUpdatedEvent(this);
                EventBus.Send(ref componentUpdated);
            }
        }

        public ProximityEffectComponent(World world, Entity entity)
        {
            World = world;
            Entity = entity;
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
    }
}