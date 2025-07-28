using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.McApiPackets
{
    public class McApiEntityDestroyedPacket : McApiPacket
    {
        public McApiEntityDestroyedPacket(string sessionToken = "", int id = 0)
        {
            SessionToken = sessionToken;
            Id = id;
        }

        public override McApiPacketType PacketType => McApiPacketType.EntityDestroyed;

        public string SessionToken { get; private set; }
        public int Id { get; private set; }

        public override void Serialize(NetDataWriter writer)
        {
            writer.Put(SessionToken, Constants.MaxStringLength);
            writer.Put(Id);
        }

        public override void Deserialize(NetDataReader reader)
        {
            SessionToken = reader.GetString(Constants.MaxStringLength);
            Id = reader.GetInt();
        }
    }
}