using LiteNetLib.Utils;

namespace VoiceCraft.Network.Packets.McApiPackets.Event;

public class McApiOnEntityMuteUpdatedPacket(int id, bool value) : IMcApiEventPacket
{
    public McApiOnEntityMuteUpdatedPacket() : this(0, false)
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

    public McApiOnEntityMuteUpdatedPacket Set(int id = 0, bool value = false)
    {
        Id = id;
        Value = value;
        return this;
    }
}