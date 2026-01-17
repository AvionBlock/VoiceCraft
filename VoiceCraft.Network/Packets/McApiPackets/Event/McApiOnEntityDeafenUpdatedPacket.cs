using LiteNetLib.Utils;
using VoiceCraft.Core;

namespace VoiceCraft.Network.Packets.McApiPackets.Event;

public class McApiOnEntityDeafenUpdatedPacket : IMcApiPacket
{
    public McApiOnEntityDeafenUpdatedPacket() : this(0, false)
    {
    }

    public McApiOnEntityDeafenUpdatedPacket(int id, bool value)
    {
        Id = id;
        Value = value;
    }

    public McApiPacketType PacketType => McApiPacketType.OnEntityDeafenUpdated;

    public int Id { get; private set; }
    public bool Value { get; private set; }

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(Id);
        writer.Put(Value);
    }

    public void Deserialize(NetDataReader reader)
    {
        Id = reader.GetInt();
        Value = reader.GetBool();
    }

    public McApiOnEntityDeafenUpdatedPacket Set(int id = 0, bool value = false)
    {
        Id = id;
        Value = value;
        return this;
    }
}