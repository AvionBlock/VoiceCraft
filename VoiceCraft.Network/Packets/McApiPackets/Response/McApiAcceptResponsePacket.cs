using LiteNetLib.Utils;
using VoiceCraft.Core;

namespace VoiceCraft.Network.Packets.McApiPackets.Response;

public class McApiAcceptResponsePacket(string requestId, string token) : IMcApiPacket, IMcApiRIdPacket
{
    public McApiAcceptResponsePacket() : this(string.Empty, string.Empty)
    {
    }

    public McApiPacketType PacketType => McApiPacketType.AcceptResponse;
    public string RequestId { get; private set; } = requestId;
    public string Token { get; private set; } = token;

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

    public void Return()
    {
        PacketPool<McApiAcceptResponsePacket>.Return(this);
    }

    public void Set(string requestId = "", string token = "")
    {
        RequestId = requestId;
        Token = token;
    }
}