using LiteNetLib.Utils;

namespace VoiceCraft.Network.Packets.McApiPackets.Event;

public class McApiOnEntityDeafenUpdatedPacket(int id, bool value) : IMcApiPacket
{
    public McApiOnEntityDeafenUpdatedPacket() : this(0, false)
    {
    }

    public McApiPacketType PacketType => McApiPacketType.OnEntityDeafenUpdated;

    public int Id { get; private set; } = id;
    public bool Value { get; private set; } = value;

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