using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.VcPackets.Request
{
    public class VcSetTitleRequestPacket : IVoiceCraftPacket
    {
        public VcSetTitleRequestPacket(): this(string.Empty)
        {
        }
        
        public VcSetTitleRequestPacket(string value)
        {
            Value = value;
        }

        public VcPacketType PacketType => VcPacketType.SetTitleRequest;

        public string Value { get; private set; }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(Value, Constants.MaxDescriptionStringLength);
        }

        public void Deserialize(NetDataReader reader)
        {
            Value = reader.GetString(Constants.MaxDescriptionStringLength);
        }
        
        public VcSetTitleRequestPacket Set(string value = "")
        {
            Value = value;
            return this;
        }
    }
}