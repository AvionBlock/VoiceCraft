using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.Packets
{
    public class RemoveComponentPacket : VoiceCraftPacket
    {
        public override PacketType PacketType => PacketType.RemoveComponent;
        public uint NetworkId { get; set; }
        public ComponentEnum ComponentType { get; set; }
        
        public override void Serialize(NetDataWriter writer)
        {
            writer.Put(NetworkId);
            writer.Put((byte)ComponentType);
        }

        public override void Deserialize(NetDataReader reader)
        {
            NetworkId = reader.GetUInt();
            ComponentType = (ComponentEnum)reader.GetByte();
        }
    }
}