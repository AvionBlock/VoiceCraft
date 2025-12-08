using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.VcPackets.Request
{
    public class VcSetListenBitmaskRequestPacket : IVoiceCraftPacket
    {
        public VcSetListenBitmaskRequestPacket() : this(0)
        {
        }

        public VcSetListenBitmaskRequestPacket(ushort value)
        {
            Value = value;
        }

        public VcPacketType PacketType => VcPacketType.SetListenBitmaskRequest;
        
        public ushort Value { get; private set; }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(Value);
        }

        public void Deserialize(NetDataReader reader)
        {
            Value = reader.GetUShort();
        }

        public VcSetListenBitmaskRequestPacket Set(ushort value = 0)
        {
            Value = value;
            return this;
        }
    }
}