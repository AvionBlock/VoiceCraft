using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.McApiPackets.Response
{
    public class McApiAcceptResponsePacket : IMcApiPacket
    {
        public McApiAcceptResponsePacket(string requestId = "", string token = "")
        {
            RequestId = requestId;
            Token = token;
        }

        public McApiPacketType PacketType => McApiPacketType.AcceptResponse;
        public string RequestId { get; private set; }
        public string Token { get; private set; }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(RequestId, Constants.MaxStringLength);
            writer.Put(Token, Constants.MaxStringLength);
        }

        public void Deserialize(NetDataReader reader)
        {
            RequestId = reader.GetString(Constants.MaxStringLength);
            Token = reader.GetString(Constants.MaxStringLength);
        }
        
        public void Set(string requestId = "", string token = "")
        {
            RequestId = requestId;
            Token = token;
        }
    }
}