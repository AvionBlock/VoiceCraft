using LiteNetLib.Utils;

namespace VoiceCraft.Network.Packets.VcPackets.Event;

public class VcOnEntityEffectBitmaskUpdatedPacket(int id, ushort value) : IVoiceCraftEventPacket
{
    public VcOnEntityEffectBitmaskUpdatedPacket() : this(0, 0)
    {
    }

    public EventType EventType => EventType.OnEntityEffectBitmaskUpdated;
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
        PacketPool<VcOnEntityEffectBitmaskUpdatedPacket>.Return(this);
    }

    public void Set(int id = 0, ushort value = 0)
    {
        Id = id;
        Value = value;
    }
}