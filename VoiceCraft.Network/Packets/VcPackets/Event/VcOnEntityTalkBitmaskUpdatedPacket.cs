using LiteNetLib.Utils;
using VoiceCraft.Core;

namespace VoiceCraft.Network.Packets.VcPackets.Event;

public class VcOnEntityTalkBitmaskUpdatedPacket : IVoiceCraftPacket
{
    public VcOnEntityTalkBitmaskUpdatedPacket() : this(0, 0)
    {
    }

    public VcOnEntityTalkBitmaskUpdatedPacket(int id, ushort value)
    {
        Id = id;
        Value = value;
    }

    public int Id { get; private set; }
    public ushort Value { get; private set; }

    public VcPacketType PacketType => VcPacketType.OnEntityTalkBitmaskUpdated;

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

    public VcOnEntityTalkBitmaskUpdatedPacket Set(int id = 0, ushort value = 0)
    {
        Id = id;
        Value = value;
        return this;
    }
}