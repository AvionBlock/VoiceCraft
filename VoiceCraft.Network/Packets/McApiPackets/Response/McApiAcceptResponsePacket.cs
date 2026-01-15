using LiteNetLib.Utils;
using VoiceCraft.Core;

namespace VoiceCraft.Network.Packets.McApiPackets.Response;

public class McApiAcceptResponsePacket : IMcApiPacket, IMcApiRIdPacket
{
    public McApiAcceptResponsePacket() : this(string.Empty, string.Empty)
    {
    }

    public McApiAcceptResponsePacket(string requestId, string token)
    {
        RequestId = requestId;
        Token = token;
    }

    public string Token { get; private set; }

    public McApiPacketType PacketType => McApiPacketType.AcceptResponse;

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

    public string RequestId { get; private set; }

    public McApiAcceptResponsePacket Set(string requestId = "", string token = "")
    {
        RequestId = requestId;
        Token = token;
        return this;
    }
}