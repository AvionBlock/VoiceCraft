using LiteNetLib.Utils;
using VoiceCraft.Core;

namespace VoiceCraft.Network.Packets.McApiPackets.Request;

public class McApiResetRequestPacket : IMcApiPacket, IMcApiRIdPacket
{
    public McApiResetRequestPacket() : this(string.Empty)
    {
    }

    public McApiResetRequestPacket(string requestId)
    {
        RequestId = requestId;
    }

    public McApiPacketType PacketType => McApiPacketType.ResetRequest;

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(RequestId, Constants.MaxStringLength);
    }

    public void Deserialize(NetDataReader reader)
    {
        RequestId = reader.GetString(Constants.MaxStringLength);
    }

    public string RequestId { get; private set; }

    public void Set(string requestId = "")
    {
        RequestId = requestId;
    }
}