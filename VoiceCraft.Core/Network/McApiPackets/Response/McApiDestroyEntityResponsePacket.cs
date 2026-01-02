using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.McApiPackets.Response
{
    public class McApiDestroyEntityResponsePacket : IMcApiPacket, IMcApiRIdPacket
    {
        public McApiDestroyEntityResponsePacket() : this(string.Empty, ResponseCodes.Ok)
        {
        }

        public McApiDestroyEntityResponsePacket(string requestId, ResponseCodes responseCode)
        {
            RequestId = requestId;
            ResponseCode = responseCode;
        }

        public McApiPacketType PacketType => McApiPacketType.DestroyEntityResponse;
        public string RequestId { get; private set; }
        public ResponseCodes ResponseCode { get; private set; }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(RequestId, Constants.MaxStringLength);
            writer.Put((sbyte)ResponseCode);
        }

        public void Deserialize(NetDataReader reader)
        {
            RequestId = reader.GetString(Constants.MaxStringLength);
            ResponseCode = (ResponseCodes)reader.GetSByte();
        }

        public McApiDestroyEntityResponsePacket Set(string requestId = "",
            ResponseCodes responseCode = ResponseCodes.Ok)
        {
            RequestId = requestId;
            ResponseCode = responseCode;
            return this;
        }

        public enum ResponseCodes : sbyte
        {
            Ok = 0,
            NotFound = -1
        }
    }
}