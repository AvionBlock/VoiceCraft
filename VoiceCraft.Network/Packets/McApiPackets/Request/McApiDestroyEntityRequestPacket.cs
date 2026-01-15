using LiteNetLib.Utils;
using VoiceCraft.Core;

namespace VoiceCraft.Network.Packets.McApiPackets.Request;

public class McApiDestroyEntityRequestPacket : IMcApiPacket, IMcApiRIdPacket
{
    public McApiDestroyEntityRequestPacket() : this(string.Empty, 0)
    {
    }

    public McApiDestroyEntityRequestPacket(string requestId, int id)
    {
        RequestId = requestId;
        Id = id;
    }

    public int Id { get; private set; }

    public McApiPacketType PacketType => McApiPacketType.DestroyEntityRequest;

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(RequestId, Constants.MaxStringLength);
        writer.Put(Id);
    }

    public void Deserialize(NetDataReader reader)
    {
        RequestId = reader.GetString(Constants.MaxStringLength);
        Id = reader.GetInt();
    }

    public string RequestId { get; private set; }

    public McApiDestroyEntityRequestPacket Set(string requestId = "", int id = 0)
    {
        RequestId = requestId;
        Id = id;
        return this;
    }
}