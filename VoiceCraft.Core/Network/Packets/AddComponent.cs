using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.Packets
{
    public class AddComponent : VoiceCraftPacket
    {
        public override PacketType PacketType => PacketType.AddComponent;
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