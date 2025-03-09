using Arch.Bus;
using Arch.Core;
using Arch.Core.Extensions;
using LiteNetLib.Utils;
using VoiceCraft.Core.Events;

namespace VoiceCraft.Core.Components
{
    public class AudioSourceComponent : IAudioOutput, IComponentSerializable
    {
        private string _environmentId = string.Empty;
        private string _name = string.Empty;
        private ulong _bitmask; //will change to a default value later.
        private IAudioInput? _audioInput;

        public World World { get; }
        public Entity Entity { get;  }
        
        public string EnvironmentId
        {
            get => _environmentId;
            set
            {
                if (_environmentId == value) return;
                _environmentId = value;
                var componentCreated = new ComponentUpdatedEvent(this);
                EventBus.Send(ref componentCreated);
            }
        }

        public string Name
        {
            get => _name;
            set
            {
                if (_name == value) return;
                _name = value;
                var componentCreated = new ComponentUpdatedEvent(this);
                EventBus.Send(ref componentCreated);
            }
        }

        public ulong Bitmask
        {
            get => _bitmask;
            set
            {
                if (_bitmask == value) return;
                _bitmask = value;
                var componentCreated = new ComponentUpdatedEvent(this);
                EventBus.Send(ref componentCreated);
            }
        }

        public IAudioInput? AudioInput
        {
            get => _audioInput;
            set
            {
                if (_audioInput == value) return;
                _audioInput = value;
                var componentCreated = new ComponentUpdatedEvent(this);
                EventBus.Send(ref componentCreated);
            }
        }

        public AudioSourceComponent(World world, Entity entity)
        {
            World = world;
            Entity = entity;
        }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(_environmentId);
            writer.Put(_name);
            writer.Put(_bitmask);

            if (!(_audioInput is IComponentSerializable componentSerializable)) return;
            if (componentSerializable.World.TryGet<NetworkComponent>(componentSerializable.Entity, out var netComponent) && netComponent != null)
            {
                writer.Put(netComponent.NetworkId); //C# compiler doesn't acknowledge null with true/false on first statement. kinda annoying.
                writer.Put(componentSerializable.GetType().Name);
            }
            
            writer.Put(uint.MaxValue); //Null value. Max value is when no entity is set.
        }

        public void Deserialize(NetDataReader reader)
        {
            _environmentId = reader.GetString();
            _name = reader.GetString();
            _bitmask = reader.GetULong();

            var networkId = reader.GetUInt();
            if(networkId == uint.MaxValue) return; //Max Value? return.
            
            var componentTypeName = reader.GetString();
            var successfullyLinked = false;
            
            //Query and link.
            var query = new QueryDescription()
                .WithAll<NetworkComponent>();
            World.Query(in query, (ref Entity entity, ref NetworkComponent networkComponent) =>
            {
                if(networkComponent.NetworkId != networkId) return;
                var components = entity.GetAllComponents();
                foreach (var component in components)
                {
                    if (component?.GetType().Name != componentTypeName || !(component is IAudioInput audioInput)) continue;
                    _audioInput = audioInput;
                    successfullyLinked = true;
                    return;
                }
            });
            
            if (successfullyLinked) return;
            _audioInput = null;
        }
    }
}