using LiteNetLib.Utils;

namespace VoiceCraft.Network.Packets.McApiPackets.Event;

public class McApiOnEntityDestroyedPacket(int id) : IMcApiPacket
{
    public McApiOnEntityDestroyedPacket() : this(0)
    {
    }

    public int Id { get; private set; } = id;

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