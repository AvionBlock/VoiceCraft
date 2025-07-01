using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.McApiPackets
{
    public class McApiAcceptPacket : McApiPacket
    {
        public override McApiPacketType PacketType => McApiPacketType.Accept;
        public string SessionToken { get; private set; }

        public McApiAcceptPacket(string sessionToken = "")
        {
            SessionToken = sessionToken;
        }

        public override void Serialize(NetDataWriter writer)
        {
            writer.Put(SessionToken);
        }

        public override void Deserialize(NetDataReader reader)
        {
            SessionToken = reader.GetString(Constants.MaxStringLength);
        }
    }
}