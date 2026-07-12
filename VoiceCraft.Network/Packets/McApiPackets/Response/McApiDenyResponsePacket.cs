using LiteNetLib.Utils;
using VoiceCraft.Core;

namespace VoiceCraft.Network.Packets.McApiPackets.Response;

public class McApiDenyResponsePacket(string requestId, string reason) : IMcApiPacket, IMcApiRIdPacket
{
    public McApiDenyResponsePacket() : this(string.Empty, string.Empty)
    {
    }

    public McApiPacketType PacketType => McApiPacketType.DenyResponse;
    public string RequestId { get; private set; } = requestId;
    public string Reason { get; private set; } = reason;

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

    
    public void Return()
    {
        PacketPool<McApiDenyResponsePacket>.Return(this);
    }

    public void Set(string requestId = "", string reasonKey = "")
    {
        RequestId = requestId;
        Reason = reasonKey;
    }
}