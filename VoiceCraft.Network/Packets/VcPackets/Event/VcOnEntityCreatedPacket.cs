using LiteNetLib.Utils;
using VoiceCraft.Core.World;

namespace VoiceCraft.Network.Packets.VcPackets.Event;

public class VcOnEntityCreatedPacket(int id) : IVoiceCraftEventPacket
{
    public VcOnEntityCreatedPacket() : this(0)
    {
    }

    public VcOnEntityCreatedPacket(VoiceCraftEntity entity) : this(entity.Id)
    {
    }

    public virtual EventType EventType => EventType.OnEntityCreated;
    public int Id { get; private set; } = id;


    public virtual void Serialize(NetDataWriter writer)
    {
        writer.Put(Id);
    }

    public virtual void Deserialize(NetDataReader reader)
    {
        Id = reader.GetInt();
    }
    
    public virtual void Return()
    {
        PacketPool<VcOnEntityCreatedPacket>.Return(this);
    }

    public void Set(int id = 0)
    {
        Id = id;
    }

    public void Set(VoiceCraftEntity entity)
    {
        Id = entity.Id;
    }
}