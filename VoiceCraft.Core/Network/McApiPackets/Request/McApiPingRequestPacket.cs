using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.McApiPackets.Request
{
    public class McApiPingRequestPacket : IMcApiPacket
    {
        public McApiPingRequestPacket(string token = "")
        {
            Token = token;
        }

        public McApiPacketType PacketType => McApiPacketType.PingRequest;
        public string Token { get; private set; }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(Token, Constants.MaxStringLength);
        }

        public void Deserialize(NetDataReader reader)
        {
            Token = reader.GetString(Constants.MaxStringLength);
        }
        
        public McApiPingRequestPacket Set(string token = "")
        {
            Token = token;
            return this;
        }
    }
}