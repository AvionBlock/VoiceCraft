using LiteNetLib.Utils;
using VoiceCraft.Core;

namespace VoiceCraft.Network.Packets.McApiPackets.Event;
public class McApiOnEntityServerMuteUpdatedPacket : IMcApiPacket
{
    public McApiOnEntityServerMuteUpdatedPacket() : this(0, false)
    {
    }

    public McApiOnEntityServerMuteUpdatedPacket(int id, bool value)
    {
        Id = id;
        Value = value;
    }

    public int Id { get; private set; }
    public bool Value { get; private set; }

    public McApiPacketType PacketType => McApiPacketType.OnEntityServerMuteUpdated;

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

    public McApiOnEntityServerMuteUpdatedPacket Set(int id = 0, bool value = false)
    {
        Id = id;
        Value = value;
        return this;
    }
}