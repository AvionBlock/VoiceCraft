using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.McApiPackets.Response
{
    public class McApiDenyResponsePacket : IMcApiPacket
    {
        public McApiDenyResponsePacket(string requestId = "", string token = "", string reasonKey = "")
        {
            RequestId = requestId;
            Token = token;
            ReasonKey = reasonKey;
        }

        public McApiPacketType PacketType => McApiPacketType.DenyResponse;
        public string RequestId { get; private set; }
        public string Token { get; private set; }
        public string ReasonKey { get; private set; }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(RequestId, Constants.MaxStringLength);
            writer.Put(Token, Constants.MaxStringLength);
            writer.Put(ReasonKey, Constants.MaxStringLength);
        }

        public void Deserialize(NetDataReader reader)
        {
            RequestId = reader.GetString(Constants.MaxStringLength);
            Token = reader.GetString(Constants.MaxStringLength);
            ReasonKey = reader.GetString(Constants.MaxStringLength);
        }
        
        public void Set(string requestId = "", string token = "", string reasonKey = "")
        {
            RequestId = requestId;
            Token = token;
            ReasonKey = reasonKey;
        }
    }
}