using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.McApiPackets
{
    public class McApiLogoutPacket: McApiPacket
    {
        public override McApiPacketType PacketType => McApiPacketType.Logout;
        public string SessionToken { get; private set; }

        public McApiLogoutPacket(string sessionToken = "")
        {
            SessionToken = sessionToken;
        }
        
        public override void Serialize(NetDataWriter writer)
        {
            writer.Put(SessionToken, Constants.MaxStringLength);
        }

        public override void Deserialize(NetDataReader reader)
        {
            SessionToken = reader.GetString(Constants.MaxStringLength);
        }
    }
}