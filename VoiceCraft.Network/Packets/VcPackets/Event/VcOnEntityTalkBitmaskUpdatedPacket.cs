using LiteNetLib.Utils;

namespace VoiceCraft.Network.Packets.VcPackets.Event;

public class VcOnEntityTalkBitmaskUpdatedPacket(int id, ushort value) : IVoiceCraftPacket
{
    public VcOnEntityTalkBitmaskUpdatedPacket() : this(0, 0)
    {
    }

    public int Id { get; private set; } = id;
    public ushort Value { get; private set; } = value;

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