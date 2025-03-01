using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.Packets
{
    public class RemoveComponent : VoiceCraftPacket
    {
        public override PacketType PacketType => PacketType.RemoveComponent;
        public ComponentEnum ComponentType { get; set; }
        
        public override void Serialize(NetDataWriter writer)
        {
            writer.Put((byte)ComponentType);
        }

        public override void Deserialize(NetDataReader reader)
        {
            ComponentType = (ComponentEnum)reader.GetByte();
        }
    }
}