using LiteNetLib.Utils;
using VoiceCraft.Core;

namespace VoiceCraft.Network.Packets.VcPackets.Event;

public class VcOnEntityListenBitmaskUpdatedPacket : IVoiceCraftPacket
{
    public VcOnEntityListenBitmaskUpdatedPacket() : this(0, 0)
    {
    }

    public VcOnEntityListenBitmaskUpdatedPacket(int id, ushort value)
    {
        Id = id;
        Value = value;
    }

    public int Id { get; private set; }
    public ushort Value { get; private set; }

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