using LiteNetLib.Utils;
using VoiceCraft.Core;

namespace VoiceCraft.Network.Packets.McApiPackets.Response;

public class McApiCreateEntityResponsePacket : IMcApiPacket, IMcApiRIdPacket
{
    public enum ResponseCodes : sbyte
    {
        Ok = 0,
        Failure = -1
    }

    public McApiCreateEntityResponsePacket() : this(string.Empty, ResponseCodes.Ok, 0)
    {
    }

    public McApiCreateEntityResponsePacket(string requestId, ResponseCodes responseCode, int id)
    {
        RequestId = requestId;
        ResponseCode = responseCode;
        Id = id;
    }

    public ResponseCodes ResponseCode { get; set; }
    public int Id { get; private set; }

    public McApiPacketType PacketType => McApiPacketType.CreateEntityResponse;

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

    public string RequestId { get; private set; }

    public McApiCreateEntityResponsePacket Set(string requestId = "", ResponseCodes responseCode = ResponseCodes.Ok,
        int id = 0)
    {
        RequestId = requestId;
        ResponseCode = responseCode;
        Id = id;
        return this;
    }
}