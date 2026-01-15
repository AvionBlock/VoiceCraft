using LiteNetLib.Utils;
using VoiceCraft.Core;

namespace VoiceCraft.Network.Packets.McApiPackets.Event;

public class McApiOnEntityTalkBitmaskUpdatedPacket : IMcApiPacket
{
    public McApiOnEntityTalkBitmaskUpdatedPacket() : this(0, 0)
    {
    }

    public McApiOnEntityTalkBitmaskUpdatedPacket(int id, ushort value)
    {
        Id = id;
        Value = value;
    }

    public int Id { get; private set; }
    public ushort Value { get; private set; }

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