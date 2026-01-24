using LiteNetLib.Utils;

namespace VoiceCraft.Network.Packets.VcPackets.Event;

public class VcOnEntityDeafenUpdatedPacket(int id, bool value) : IVoiceCraftPacket
{
    public VcOnEntityDeafenUpdatedPacket() : this(0, false)
    {
    }

    public int Id { get; private set; } = id;
    public bool Value { get; private set; } = value;

    public VcPacketType PacketType => VcPacketType.OnEntityDeafenUpdated;

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

    public VcOnEntityDeafenUpdatedPacket Set(int id = 0, bool value = false)
    {
        Id = id;
        Value = value;
        return this;
    }
}