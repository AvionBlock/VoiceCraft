using LiteNetLib.Utils;

namespace VoiceCraft.Network.Packets.VcPackets.Event;

public class VcOnEntityTalkBitmaskUpdatedPacket(int id, ushort value) : IVoiceCraftEventPacket
{
    public VcOnEntityTalkBitmaskUpdatedPacket() : this(0, 0)
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
        PacketPool<VcOnEntityTalkBitmaskUpdatedPacket>.Return(this);
    }

    public void Set(int id = 0, ushort value = 0)
    {
        Id = id;
        Value = value;
    }
}