using LiteNetLib.Utils;
using VoiceCraft.Core;

namespace VoiceCraft.Network.Packets.McApiPackets.Response;

public class McApiCreateEntityResponsePacket(
    string requestId,
    McApiCreateEntityResponsePacket.ResponseCodes responseCode,
    int id)
    : IMcApiPacket, IMcApiRIdPacket
{
    public enum ResponseCodes : sbyte
    {
        Ok = 0,
        Failure = -1
    }

    public McApiCreateEntityResponsePacket() : this(string.Empty, ResponseCodes.Ok, 0)
    {
    }

    public ResponseCodes ResponseCode { get; set; } = responseCode;
    public int Id { get; private set; } = id;

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

    public string RequestId { get; private set; } = requestId;

    public McApiCreateEntityResponsePacket Set(string requestId = "", ResponseCodes responseCode = ResponseCodes.Ok,
        int id = 0)
    {
        RequestId = requestId;
        ResponseCode = responseCode;
        Id = id;
        return this;
    }
}