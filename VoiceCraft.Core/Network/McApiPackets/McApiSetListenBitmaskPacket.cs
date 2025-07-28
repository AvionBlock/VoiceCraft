using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.McApiPackets
{
    public class McApiSetListenBitmaskPacket : McApiPacket
    {
        public McApiSetListenBitmaskPacket(string sessionToken = "", int id = 0, ulong value = 0)
        {
            SessionToken = sessionToken;
            Id = id;
            Value = value;
        }

        public override McApiPacketType PacketType => McApiPacketType.SetListenBitmask;

        public string SessionToken { get; private set; }
        public int Id { get; private set; }
        public ulong Value { get; private set; }

        public override void Serialize(NetDataWriter writer)
        {
            writer.Put(SessionToken, Constants.MaxStringLength);
            writer.Put(Id);
            writer.Put(Value);
        }

        public override void Deserialize(NetDataReader reader)
        {
            SessionToken = reader.GetString(Constants.MaxStringLength);
            Id = reader.GetInt();
            Value = reader.GetULong();
        }
    }
}