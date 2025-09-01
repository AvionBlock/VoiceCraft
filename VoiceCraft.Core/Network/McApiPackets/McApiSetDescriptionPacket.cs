using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.McApiPackets
{
    public class McApiSetDescriptionPacket : McApiPacket
    {
        public McApiSetDescriptionPacket(string sessionToken = "", int id = 0, string value = "")
        {
            SessionToken = sessionToken;
            Id = id;
            Value = value;
        }

        public override McApiPacketType PacketType => McApiPacketType.SetDescription;

        public string SessionToken { get; private set; }
        public int Id { get; private set; }
        public string Value { get; private set; }

        public override void Serialize(NetDataWriter writer)
        {
            writer.Put(SessionToken, Constants.MaxStringLength);
            writer.Put(Id);
            writer.Put(Value, Constants.MaxDescriptionStringLength);
        }

        public override void Deserialize(NetDataReader reader)
        {
            SessionToken = reader.GetString(Constants.MaxStringLength);
            Id = reader.GetInt();
            Value = reader.GetString(Constants.MaxDescriptionStringLength);
        }
    }
}