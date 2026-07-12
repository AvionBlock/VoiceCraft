using System.Numerics;
using LiteNetLib.Utils;
using VoiceCraft.Core;
using VoiceCraft.Core.World;

namespace VoiceCraft.Network.Packets.McApiPackets.Request;

public class McApiCreateEntityRequestPacket(string requestId) : IMcApiPacket, IMcApiRIdPacket
{
    public McApiCreateEntityRequestPacket() : this(string.Empty)
    {
    }

    public string RequestId { get; private set; } = requestId;

    public McApiPacketType PacketType => McApiPacketType.CreateEntityRequest;

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(RequestId, Constants.MaxStringLength);
    }

    public void Deserialize(NetDataReader reader)
    {
        RequestId = reader.GetString(Constants.MaxStringLength);
    }

    public void Return()
    {
        PacketPool<McApiCreateEntityRequestPacket>.Return(this);
    }

    public void Set(string requestId = "")
    {
        RequestId = requestId;
    }
}