using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.McApiPackets
{
    public class McApiSetTitlePacket : McApiPacket
    {
        public McApiSetTitlePacket(string sessionToken = "", string value = "")
        {
            SessionToken = sessionToken;
            Value = value;
        }

        public override McApiPacketType PacketType => McApiPacketType.SetTitle;

        public string SessionToken { get; private set; }
        public string Value { get; private set; }

        public override void Serialize(NetDataWriter writer)
        {
            writer.Put(SessionToken, Constants.MaxStringLength);
            writer.Put(Value, Constants.MaxStringLength);
        }

        public override void Deserialize(NetDataReader reader)
        {
            SessionToken = reader.GetString(Constants.MaxStringLength);
            Value = reader.GetString(Constants.MaxStringLength);
        }
    }
}