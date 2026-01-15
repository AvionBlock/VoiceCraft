using LiteNetLib.Utils;
using VoiceCraft.Core;

namespace VoiceCraft.Network.Packets.McApiPackets.Response;

public class McApiDenyResponsePacket : IMcApiPacket, IMcApiRIdPacket
{
    public McApiDenyResponsePacket() : this(string.Empty, string.Empty)
    {
    }

    public McApiDenyResponsePacket(string requestId, string reason)
    {
        RequestId = requestId;
        Reason = reason;
    }

    public string Reason { get; private set; }

    public McApiPacketType PacketType => McApiPacketType.DenyResponse;

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(RequestId, Constants.MaxStringLength);
        writer.Put(Reason, Constants.MaxStringLength);
    }

    public void Deserialize(NetDataReader reader)
    {
        RequestId = reader.GetString(Constants.MaxStringLength);
        Reason = reader.GetString(Constants.MaxStringLength);
    }

    public string RequestId { get; private set; }

    public McApiDenyResponsePacket Set(string requestId = "", string reasonKey = "")
    {
        RequestId = requestId;
        Reason = reasonKey;
        return this;
    }
}