using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.VcPackets.Request
{
    public class VcSetNameRequestPacket : IVoiceCraftPacket
    {
        public VcSetNameRequestPacket() : this(string.Empty)
        {
        }

        public VcSetNameRequestPacket(string value)
        {
            Value = value;
        }

        public VcPacketType PacketType => VcPacketType.SetNameRequest;

        public string Value { get; private set; }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(Value, Constants.MaxStringLength);
        }

        public void Deserialize(NetDataReader reader)
        {
            Value = reader.GetString(Constants.MaxStringLength);
        }

        public VcSetNameRequestPacket Set(string value = "")
        {
            Value = value;
            return this;
        }
    }
}