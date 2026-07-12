using LiteNetLib.Utils;

namespace VoiceCraft.Network.Packets.VcPackets.Event;

public class VcOnEntityMuteUpdatedPacket(int id, bool value) : IVoiceCraftEventPacket
{
    public VcOnEntityMuteUpdatedPacket() : this(0, false)
    {
    }

    public EventType EventType => EventType.OnEntityMuteUpdated;
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
        PacketPool<VcOnEntityMuteUpdatedPacket>.Return(this);
    }

    public void Set(int id = 0, bool value = false)
    {
        Id = id;
        Value = value;
    }
}