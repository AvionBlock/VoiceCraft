using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.VcPackets.Request
{
    public class VcSetServerMuteRequestPacket : IVoiceCraftPacket
    {
        public VcSetServerMuteRequestPacket() : this(false)
        {
        }

        public VcSetServerMuteRequestPacket(bool value)
        {
            Value = value;
        }

        public VcPacketType PacketType => VcPacketType.SetServerMuteRequest;

        public bool Value { get; private set; }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(Value);
        }

        public void Deserialize(NetDataReader reader)
        {
            Value = reader.GetBool();
        }

        public VcSetServerMuteRequestPacket Set(bool value = false)
        {
            Value = value;
            return this;
        }
    }
}