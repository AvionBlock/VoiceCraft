using LiteNetLib.Utils;

namespace VoiceCraft.Network.Packets.VcPackets.Event;

public class VcOnEntityCaveFactorUpdatedPacket(int id, float value) : IVoiceCraftPacket
{
    public VcOnEntityCaveFactorUpdatedPacket() : this(0, 0.0f)
    {
    }

    public int Id { get; private set; } = id;
    public float Value { get; private set; } = value;

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