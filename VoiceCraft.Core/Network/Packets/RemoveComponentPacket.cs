using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.Packets
{
    public class RemoveComponentPacket : VoiceCraftPacket
    {
        public override PacketType PacketType => PacketType.RemoveComponent;
        public int NetworkId { get; set; }
        public ComponentType ComponentType { get; set; } = ComponentType.Unknown;
        
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