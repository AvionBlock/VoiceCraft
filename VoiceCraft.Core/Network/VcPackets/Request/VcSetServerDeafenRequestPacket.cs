using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.VcPackets.Request
{
    public class VcSetServerDeafenRequestPacket : IVoiceCraftPacket
    {
        public VcSetServerDeafenRequestPacket() : this(false)
        {
        }

        public VcSetServerDeafenRequestPacket(bool value)
        {
            Value = value;
        }

        public VcPacketType PacketType => VcPacketType.SetServerDeafenRequest;

        public bool Value { get; private set; }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(Value);
        }

        public void Deserialize(NetDataReader reader)
        {
            Value = reader.GetBool();
        }

        public VcSetServerDeafenRequestPacket Set(bool value = true)
        {
            Value = value;
            return this;
        }
    }
}