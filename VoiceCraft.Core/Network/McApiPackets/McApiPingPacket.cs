using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.McApiPackets
{
    public class McApiPingPacket : McApiPacket
    {
        public override McApiPacketType PacketType => McApiPacketType.Ping;
        public string SessionToken { get; private set; }

        public McApiPingPacket(string sessionToken = "")
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