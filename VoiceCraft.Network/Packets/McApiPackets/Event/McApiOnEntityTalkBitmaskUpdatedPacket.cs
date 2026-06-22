using LiteNetLib.Utils;

namespace VoiceCraft.Network.Packets.McApiPackets.Event;

public class McApiOnEntityTalkBitmaskUpdatedPacket(int id, ushort value) : IMcApiEventPacket
{
    public McApiOnEntityTalkBitmaskUpdatedPacket() : this(0, 0)
    {
    }

    public EventType EventType => EventType.OnEntityTalkBitmaskUpdated;
    public int Id { get; private set; } = id;
    public ushort Value { get; private set; } = value;

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(Id);
        writer.Put(Value);
    }

    public void Deserialize(NetDataReader reader)
    {
        Id = reader.GetInt();
        Value = reader.GetUShort();
    }
    
    public void Return()
    {
        PacketPool<McApiOnEntityTalkBitmaskUpdatedPacket>.Return(this);
    }

    public McApiOnEntityTalkBitmaskUpdatedPacket Set(int id = 0, ushort value = 0)
    {
        Id = id;
        Value = value;
        return this;
    }
}