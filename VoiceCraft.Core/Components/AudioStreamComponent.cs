using Arch.Core;
using LiteNetLib.Utils;

namespace VoiceCraft.Core.Components
{
    public class AudioStreamComponent : IAudioInput, IComponentSerializable
    {
        public World World { get; }
        public Entity Entity { get; }
        
        public AudioStreamComponent(World world, Entity entity)
        {
            World = world;
            Entity = entity;
        }
        
        public void Serialize(NetDataWriter writer)
        {
            //Do absolutely nothing.
        }

        public void Deserialize(NetDataReader reader)
        {
            //Do absolutely nothing.
        }
    }
}