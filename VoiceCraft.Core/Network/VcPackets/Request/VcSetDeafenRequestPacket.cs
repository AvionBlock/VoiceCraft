using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.VcPackets.Request
{
    public class VcSetDeafenRequestPacket : IVoiceCraftPacket
    {
        public VcSetDeafenRequestPacket() : this(false)
        {
        }
        
        public VcSetDeafenRequestPacket(bool value)
        {
            Value = value;
        }

        public VcPacketType PacketType => VcPacketType.SetDeafenRequest;
        
        public bool Value { get; private set; }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(Value);
        }

        public void Deserialize(NetDataReader reader)
        {
            Value = reader.GetBool();
        }
        
        public VcSetDeafenRequestPacket Set(bool value = true)
        {
            Value = value;
            return this;
        }
    }
}