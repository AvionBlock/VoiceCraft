using LiteNetLib.Utils;

namespace VoiceCraft.Network.Packets.McApiPackets.Event;

public class McApiOnEntityTalkBitmaskUpdatedPacket(int id, ushort value) : IMcApiPacket
{
    public McApiOnEntityTalkBitmaskUpdatedPacket() : this(0, 0)
    {
    }

    public int Id { get; private set; } = id;
    public ushort Value { get; private set; } = value;

    public McApiPacketType PacketType => McApiPacketType.OnEntityTalkBitmaskUpdated;

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

    public McApiOnEntityTalkBitmaskUpdatedPacket Set(int id = 0, ushort value = 0)
    {
        Id = id;
        Value = value;
        return this;
    }
}