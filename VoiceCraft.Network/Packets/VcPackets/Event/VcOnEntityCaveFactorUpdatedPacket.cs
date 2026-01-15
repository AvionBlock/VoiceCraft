using LiteNetLib.Utils;
using VoiceCraft.Core;

namespace VoiceCraft.Network.Packets.VcPackets.Event;

public class VcOnEntityCaveFactorUpdatedPacket : IVoiceCraftPacket
{
    public VcOnEntityCaveFactorUpdatedPacket() : this(0, 0.0f)
    {
    }

    public VcOnEntityCaveFactorUpdatedPacket(int id, float value)
    {
        Id = id;
        Value = value;
    }

    public int Id { get; private set; }
    public float Value { get; private set; }

    public VcPacketType PacketType => VcPacketType.OnEntityCaveFactorUpdated;

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

    public VcOnEntityCaveFactorUpdatedPacket Set(int id = 0, float value = 0f)
    {
        Id = id;
        Value = value;
        return this;
    }
}