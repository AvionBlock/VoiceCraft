using Arch.Bus;
using Arch.Core;
using LiteNetLib.Utils;
using VoiceCraft.Core.Events;

namespace VoiceCraft.Core.Components
{
    public class AudioListenerComponent : IAudioInput, IComponentSerializable
    {
        private string _environmentId = string.Empty;
        private ulong _bitmask; //Will change to a default value later.

        public World World { get; }
        public Entity Entity { get; }
        
        public string EnvironmentId
        {
            get => _environmentId;
            set
            {
                if (_environmentId == value) return;
                _environmentId = value;
                var componentUpdated = new ComponentUpdatedEvent(this);
                EventBus.Send(ref componentUpdated);
            }
        }

        public ulong Bitmask
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

        public AudioListenerComponent(World world, Entity entity)
        {
            World = world;
            Entity = entity;
        }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(EnvironmentId);
            writer.Put(Bitmask);
        }

        public void Deserialize(NetDataReader reader)
        {
            _environmentId = reader.GetString();
            _bitmask = reader.GetULong();
        }
    }
}