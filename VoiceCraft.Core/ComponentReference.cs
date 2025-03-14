using LiteNetLib.Utils;
using VoiceCraft.Core.Network;

namespace VoiceCraft.Core
{
    public struct ComponentReference : INetSerializable
    {
        public int NetworkId { get; set; }
        public ComponentType ComponentType { get; set; }

        public ComponentReference(int networkId, ComponentType componentType = ComponentType.Unknown)
        {
            NetworkId = networkId;
            ComponentType = componentType;
        }
        
        public void Serialize(NetDataWriter writer)
        {
            writer.Put(NetworkId);
            writer.Put((byte)ComponentType);
        }

        public void Deserialize(NetDataReader reader)
        {
            NetworkId = reader.GetInt();
            ComponentType = (ComponentType)reader.GetByte();
        }
    }
}