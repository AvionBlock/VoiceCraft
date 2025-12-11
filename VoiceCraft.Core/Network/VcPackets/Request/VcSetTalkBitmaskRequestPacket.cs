using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.VcPackets.Request
{
    public class VcSetTalkBitmaskRequestPacket : IVoiceCraftPacket
    {
        public VcSetTalkBitmaskRequestPacket() : this(0)
        {
        }

        public VcSetTalkBitmaskRequestPacket(ushort value)
        {
            Value = value;
        }

        public VcPacketType PacketType => VcPacketType.SetTalkBitmaskRequest;
        
        public ushort Value { get; private set; }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(Value);
        }

        public void Deserialize(NetDataReader reader)
        {
            Value = reader.GetUShort();
        }

        public VcSetTalkBitmaskRequestPacket Set(ushort value = 0)
        {
            Value = value;
            return this;
        }
    }
}