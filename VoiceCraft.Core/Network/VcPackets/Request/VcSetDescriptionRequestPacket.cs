using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.VcPackets.Request
{
    public class VcSetDescriptionRequestPacket : IVoiceCraftPacket
    {
        public VcSetDescriptionRequestPacket() : this(string.Empty)
        {
        }
        
        public VcSetDescriptionRequestPacket(string value)
        {
            Value = value;
        }

        public VcPacketType PacketType => VcPacketType.SetDescriptionRequest;

        public string Value { get; private set; }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(Value, Constants.MaxDescriptionStringLength);
        }

        public void Deserialize(NetDataReader reader)
        {
            Value = reader.GetString(Constants.MaxDescriptionStringLength);
        }
        
        public VcSetDescriptionRequestPacket Set(string value = "")
        {
            Value = value;
            return this;
        }
    }
}