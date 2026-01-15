using LiteNetLib.Utils;
using VoiceCraft.Core;

namespace VoiceCraft.Network.Packets.McApiPackets.Event;

public class McApiOnEntityDestroyedPacket : IMcApiPacket
{
    public McApiOnEntityDestroyedPacket() : this(0)
    {
    }

    public McApiOnEntityDestroyedPacket(int id)
    {
        Id = id;
    }

    public int Id { get; private set; }

    public McApiPacketType PacketType => McApiPacketType.OnEntityDestroyed;

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(Id);
    }

    public void Deserialize(NetDataReader reader)
    {
        Id = reader.GetInt();
    }

    public McApiOnEntityDestroyedPacket Set(int id = 0)
    {
        Id = id;
        return this;
    }
}