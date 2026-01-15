using LiteNetLib.Utils;
using VoiceCraft.Core;

namespace VoiceCraft.Network.Packets.VcPackets.Event;

public class VcOnEntityEffectBitmaskUpdatedPacket : IVoiceCraftPacket
{
    public VcOnEntityEffectBitmaskUpdatedPacket() : this(0, 0)
    {
    }

    public VcOnEntityEffectBitmaskUpdatedPacket(int id, ushort value)
    {
        Id = id;
        Value = value;
    }

    public int Id { get; private set; }
    public ushort Value { get; private set; }

    public VcPacketType PacketType => VcPacketType.OnEntityEffectBitmaskUpdated;

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

    public VcOnEntityEffectBitmaskUpdatedPacket Set(int id = 0, ushort value = 0)
    {
        Id = id;
        Value = value;
        return this;
    }
}