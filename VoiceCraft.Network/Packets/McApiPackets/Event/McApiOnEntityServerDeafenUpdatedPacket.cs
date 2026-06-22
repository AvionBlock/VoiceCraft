using LiteNetLib.Utils;

namespace VoiceCraft.Network.Packets.McApiPackets.Event;

public class McApiOnEntityServerDeafenUpdatedPacket(int id, bool value) : IMcApiEventPacket
{
    public McApiOnEntityServerDeafenUpdatedPacket() : this(0, false)
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
        PacketPool<McApiOnEntityServerDeafenUpdatedPacket>.Return(this);
    }

    public McApiOnEntityServerDeafenUpdatedPacket Set(int id = 0, bool value = false)
    {
        Id = id;
        Value = value;
        return this;
    }
}