using LiteNetLib.Utils;
using VoiceCraft.Core;

namespace VoiceCraft.Network.Packets.VcPackets.Event;

public class VcOnEntityDeafenUpdatedPacket : IVoiceCraftPacket
{
    public VcOnEntityDeafenUpdatedPacket() : this(0, false)
    {
    }

    public VcOnEntityDeafenUpdatedPacket(int id, bool value)
    {
        Id = id;
        Value = value;
    }

    public int Id { get; private set; }
    public bool Value { get; private set; }

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