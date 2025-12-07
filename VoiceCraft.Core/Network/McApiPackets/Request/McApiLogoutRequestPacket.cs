using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.McApiPackets.Request
{
    public class McApiLogoutRequestPacket : IMcApiPacket
    {
        public McApiLogoutRequestPacket(string token = "")
        {
            Token = token;
        }

        public McApiPacketType PacketType => McApiPacketType.LogoutRequest;
        public string Token { get; private set; }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(Token, Constants.MaxStringLength);
        }

        public void Deserialize(NetDataReader reader)
        {
            Token = reader.GetString(Constants.MaxStringLength);
        }
        
        public McApiLogoutRequestPacket Set(string token = "")
        {
            Token = token;
            return this;
        }
    }
}