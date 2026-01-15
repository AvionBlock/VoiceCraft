using LiteNetLib.Utils;
using VoiceCraft.Core;

namespace VoiceCraft.Network.Packets.McApiPackets.Event;

public class McApiOnEntityMuffleFactorUpdatedPacket : IMcApiPacket
{
    public McApiOnEntityMuffleFactorUpdatedPacket() : this(0, 0.0f)
    {
    }

    public McApiOnEntityMuffleFactorUpdatedPacket(int id, float value)
    {
        Id = id;
        Value = value;
    }

    public int Id { get; private set; }
    public float Value { get; private set; }

    public McApiPacketType PacketType => McApiPacketType.OnEntityMuffleFactorUpdated;

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(Id);
        writer.Put(Value);
    }

    public void Deserialize(NetDataReader reader)
    {
        Id = reader.GetInt();
        Value = reader.GetFloat();
    }

    public McApiOnEntityMuffleFactorUpdatedPacket Set(int id = 0, float value = 0.0f)
    {
        Id = id;
        Value = value;
        return this;
    }
}