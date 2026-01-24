using LiteNetLib.Utils;

namespace VoiceCraft.Network.Packets.McApiPackets.Event;

public class McApiOnEntityServerDeafenUpdatedPacket(int id, bool value) : IMcApiPacket
{
    public McApiOnEntityServerDeafenUpdatedPacket() : this(0, false)
    {
    }

    public int Id { get; private set; } = id;
    public bool Value { get; private set; } = value;

    public McApiPacketType PacketType => McApiPacketType.OnEntityServerDeafenUpdated;

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

    public McApiOnEntityServerDeafenUpdatedPacket Set(int id = 0, bool value = false)
    {
        Id = id;
        Value = value;
        return this;
    }
}