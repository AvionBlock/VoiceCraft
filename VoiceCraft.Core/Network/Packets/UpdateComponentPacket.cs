using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.Packets
{
    public class UpdateComponentPacket : VoiceCraftPacket
    {
        private ComponentType _componentType;
        
        public override PacketType PacketType => PacketType.RemoveComponent;
        public int NetworkId { get; set; }
        public ComponentType ComponentType { get => Component?.ComponentType ?? _componentType; set => _componentType = value; }
        public ISerializableEntityComponent? Component { get; set; }
        
        public override void Serialize(NetDataWriter writer)
        {
            writer.Put(NetworkId);
            writer.Put((byte)ComponentType);
        }

        public override void Deserialize(NetDataReader reader)
        {
            NetworkId = reader.GetInt();
            ComponentType = (ComponentType)reader.GetByte();
        }
    }
}