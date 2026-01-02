using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.McApiPackets.Response
{
    public class McApiCreateEntityResponsePacket : IMcApiPacket, IMcApiRIdPacket
    {
        public McApiCreateEntityResponsePacket() : this(string.Empty, ResponseCodes.Ok, 0)
        {
        }

        public McApiCreateEntityResponsePacket(string requestId, ResponseCodes responseCode, int id)
        {
            RequestId = requestId;
            ResponseCode = responseCode;
            Id = id;
        }

        public McApiPacketType PacketType => McApiPacketType.CreateEntityResponse;
        public string RequestId { get; private set; }
        public ResponseCodes ResponseCode { get; set; }
        public int Id { get; private set; }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(RequestId, Constants.MaxStringLength);
            writer.Put((sbyte)ResponseCode);
            writer.Put(Id);
        }

        public void Deserialize(NetDataReader reader)
        {
            RequestId = reader.GetString(Constants.MaxStringLength);
            ResponseCode = (ResponseCodes)reader.GetSByte();
            Id = reader.GetInt();
        }

        public McApiCreateEntityResponsePacket Set(string requestId = "", ResponseCodes responseCode = ResponseCodes.Ok,
            int id = 0)
        {
            RequestId = requestId;
            ResponseCode = responseCode;
            Id = id;
            return this;
        }

        public enum ResponseCodes : sbyte
        {
            Ok = 0,
            Failure = -1
        }
    }
}