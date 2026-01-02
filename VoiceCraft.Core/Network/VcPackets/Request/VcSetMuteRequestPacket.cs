using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.VcPackets.Request
{
    public class VcSetMuteRequestPacket : IVoiceCraftPacket
    {
        public VcSetMuteRequestPacket() : this(false)
        {
        }

        public VcSetMuteRequestPacket(bool value)
        {
            Value = value;
        }

        public VcPacketType PacketType => VcPacketType.SetMuteRequest;

        public bool Value { get; private set; }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(Value);
        }

        public void Deserialize(NetDataReader reader)
        {
            Value = reader.GetBool();
        }

        public VcSetMuteRequestPacket Set(bool value = false)
        {
            Value = value;
            return this;
        }
    }
}