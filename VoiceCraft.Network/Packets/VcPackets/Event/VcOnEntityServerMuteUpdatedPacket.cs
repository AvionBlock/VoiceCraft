using LiteNetLib.Utils;

namespace VoiceCraft.Network.Packets.VcPackets.Event;

public class VcOnEntityServerMuteUpdatedPacket(int id, bool value) : IVoiceCraftEventPacket
{
    public VcOnEntityServerMuteUpdatedPacket() : this(0, false)
    {
    }

    public EventType EventType => EventType.OnEntityServerMuteUpdated;
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

    public VcOnEntityServerMuteUpdatedPacket Set(int id = 0, bool value = false)
    {
        Id = id;
        Value = value;
        return this;
    }
}