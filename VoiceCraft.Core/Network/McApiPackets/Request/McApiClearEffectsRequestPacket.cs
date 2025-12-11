using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.McApiPackets.Request
{
    public class McApiClearEffectsRequestPacket : IMcApiPacket
    {
        public McApiClearEffectsRequestPacket() : this(string.Empty)
        {
        }

        public McApiClearEffectsRequestPacket(string token = "")
        {
            Token = token;
        }

        public McApiPacketType PacketType => McApiPacketType.ClearEffectsRequest;
        public string Token { get; private set; }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(Token, Constants.MaxStringLength);
        }

        public void Deserialize(NetDataReader reader)
        {
            Token = reader.GetString(Constants.MaxStringLength);
        }

        public McApiClearEffectsRequestPacket Set(string token = "")
        {
            Token = token;
            return this;
        }
    }
}