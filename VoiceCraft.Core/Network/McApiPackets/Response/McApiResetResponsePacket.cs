using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.McApiPackets.Response
{
    public class McApiResetResponsePacket : IMcApiPacket, IMcApiRIdPacket
    {
        public McApiResetResponsePacket() : this(string.Empty, ResponseCodes.Ok)
        {
        }

        public McApiResetResponsePacket(string requestId, ResponseCodes responseCode)
        {
            RequestId = requestId;
            ResponseCode = responseCode;
        }

        public McApiPacketType PacketType => McApiPacketType.ResetResponse;
        
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

        public McApiResetResponsePacket Set(string requestId = "",
            ResponseCodes responseCode = ResponseCodes.Ok)
        {
            RequestId = requestId;
            ResponseCode = responseCode;
            return this;
        }

        public enum ResponseCodes : sbyte
        {
            Ok = 0,
            Failure = -1
        }
    }
}