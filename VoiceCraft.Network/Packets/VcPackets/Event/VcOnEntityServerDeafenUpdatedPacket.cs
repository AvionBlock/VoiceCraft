using LiteNetLib.Utils;

namespace VoiceCraft.Network.Packets.VcPackets.Event;

public class VcOnEntityServerDeafenUpdatedPacket(int id, bool value) : IVoiceCraftEventPacket
{
    public VcOnEntityServerDeafenUpdatedPacket() : this(0, false)
    {
    }

    public EventType EventType => EventType.OnEntityServerDeafenUpdated;
    public int Id { get; private set; } = id;
    public bool Value { get; private set; } = value;

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(Id);
        writer.Put(Value);
    }

    public void Deserialize(NetDataReader reader)
    {
        Id = reader.GetInt();
        Value = reader.GetBool();
    }
    
    public void Return()
    {
        PacketPool<VcOnEntityServerDeafenUpdatedPacket>.Return(this);
    }

    public void Set(int id = 0, bool value = false)
    {
        Id = id;
        Value = value;
    }
}