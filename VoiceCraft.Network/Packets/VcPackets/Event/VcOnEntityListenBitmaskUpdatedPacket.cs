using LiteNetLib.Utils;

namespace VoiceCraft.Network.Packets.VcPackets.Event;

public class VcOnEntityListenBitmaskUpdatedPacket(int id, ushort value) : IVoiceCraftPacket
{
    public VcOnEntityListenBitmaskUpdatedPacket() : this(0, 0)
    {
    }

    public int Id { get; private set; } = id;
    public ushort Value { get; private set; } = value;

    public VcPacketType PacketType => VcPacketType.OnEntityListenBitmaskUpdated;

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

    public VcOnEntityListenBitmaskUpdatedPacket Set(int id = 0, ushort value = 0)
    {
        Id = id;
        Value = value;
        return this;
    }
}