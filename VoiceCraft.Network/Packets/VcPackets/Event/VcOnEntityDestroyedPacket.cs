using LiteNetLib.Utils;

namespace VoiceCraft.Network.Packets.VcPackets.Event;

public class VcOnEntityDestroyedPacket(int id) : IVoiceCraftEventPacket
{
    public VcOnEntityDestroyedPacket() : this(0)
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
        PacketPool<VcOnEntityDestroyedPacket>.Return(this);
    }

    public VcOnEntityDestroyedPacket Set(int id = 0)
    {
        Id = id;
        return this;
    }
}