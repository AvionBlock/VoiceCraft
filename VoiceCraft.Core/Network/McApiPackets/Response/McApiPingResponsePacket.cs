using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.McApiPackets.Response
{
    public class McApiPingResponsePacket : IMcApiPacket
    {
        public McApiPingResponsePacket() : this(string.Empty)
        {
        }

        public McApiPingResponsePacket(string token = "")
        {
            Token = token;
        }

        public McApiPacketType PacketType => McApiPacketType.PingResponse;
        public string Token { get; private set; }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(Token, Constants.MaxStringLength);
        }

        public void Deserialize(NetDataReader reader)
        {
            Token = reader.GetString(Constants.MaxStringLength);
        }

        public McApiPingResponsePacket Set(string token = "")
        {
            Token = token;
            return this;
        }
    }
}