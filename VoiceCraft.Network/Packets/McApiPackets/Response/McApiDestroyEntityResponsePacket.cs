using LiteNetLib.Utils;
using VoiceCraft.Core;

namespace VoiceCraft.Network.Packets.McApiPackets.Response;

public class McApiDestroyEntityResponsePacket(
    string requestId,
    McApiDestroyEntityResponsePacket.ResponseCodes responseCode)
    : IMcApiPacket, IMcApiRIdPacket
{
    public enum ResponseCodes : sbyte
    {
        Ok = 0,
        NotFound = -1
    }

    public McApiDestroyEntityResponsePacket() : this(string.Empty, ResponseCodes.Ok)
    {
    }
    
    public McApiPacketType PacketType => McApiPacketType.DestroyEntityResponse;
    public string RequestId { get; private set; } = requestId;
    public ResponseCodes ResponseCode { get; private set; } = responseCode;

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
    
    public void Return()
    {
        PacketPool<McApiDestroyEntityResponsePacket>.Return(this);
    }

    public McApiDestroyEntityResponsePacket Set(string requestId = "",
        ResponseCodes responseCode = ResponseCodes.Ok)
    {
        RequestId = requestId;
        ResponseCode = responseCode;
        return this;
    }
}