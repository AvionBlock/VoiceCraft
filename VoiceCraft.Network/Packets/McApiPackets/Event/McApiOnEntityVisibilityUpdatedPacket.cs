using LiteNetLib.Utils;

namespace VoiceCraft.Network.Packets.McApiPackets.Event;

public class McApiOnEntityVisibilityUpdatedPacket(int id, int id2, bool value) : IMcApiPacket
{
    public McApiOnEntityVisibilityUpdatedPacket() : this(0, 0, false)
    {
    }

    public int Id { get; private set; } = id;
    public int Id2 { get; private set; } = id2;
    public bool Value { get; private set; } = value;

    public McApiPacketType PacketType => McApiPacketType.OnEntityVisibilityUpdated;

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(Id);
        writer.Put(Id2);
        writer.Put(Value);
    }

    public void Deserialize(NetDataReader reader)
    {
        Id = reader.GetInt();
        Id2 = reader.GetInt();
        Value = reader.GetBool();
    }

    public McApiOnEntityVisibilityUpdatedPacket Set(int id = 0, int id2 = 0, bool value = false)
    {
        Id = id;
        Id2 = id2;
        Value = value;
        return this;
    }
}