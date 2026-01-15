using LiteNetLib.Utils;
using VoiceCraft.Core;

namespace VoiceCraft.Network.Packets.VcPackets.Event;

public class VcOnEntityServerMuteUpdatedPacket : IVoiceCraftPacket
{
    public VcOnEntityServerMuteUpdatedPacket() : this(0, false)
    {
    }

    public VcOnEntityServerMuteUpdatedPacket(int id, bool value)
    {
        Id = id;
        Value = value;
    }

    public int Id { get; private set; }
    public bool Value { get; private set; }

    public VcPacketType PacketType => VcPacketType.OnEntityServerMuteUpdated;

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

    public VcOnEntityServerMuteUpdatedPacket Set(int id = 0, bool value = false)
    {
        Id = id;
        Value = value;
        return this;
    }
}