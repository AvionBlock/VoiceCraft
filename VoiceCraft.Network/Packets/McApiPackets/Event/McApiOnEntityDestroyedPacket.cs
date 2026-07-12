using LiteNetLib.Utils;

namespace VoiceCraft.Network.Packets.McApiPackets.Event;

public class McApiOnEntityDestroyedPacket(int id) : IMcApiEventPacket
{
    public McApiOnEntityDestroyedPacket() : this(0)
    {
    }

    public EventType EventType => EventType.OnEntityDestroyed;
    public int Id { get; private set; } = id;

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(Id);
    }

    public void Deserialize(NetDataReader reader)
    {
        Id = reader.GetInt();
    }
    
    public void Return()
    {
        PacketPool<McApiOnEntityDestroyedPacket>.Return(this);
    }

    public void Set(int id = 0)
    {
        Id = id;
    }
}