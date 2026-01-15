using LiteNetLib.Utils;
using VoiceCraft.Core;

namespace VoiceCraft.Network.Packets.McApiPackets.Event;

public class McApiOnEntityWorldIdUpdatedPacket : IMcApiPacket
{
    public McApiOnEntityWorldIdUpdatedPacket() : this(0, string.Empty)
    {
    }

    public McApiOnEntityWorldIdUpdatedPacket(int id, string value)
    {
        Id = id;
        Value = value;
    }

    public int Id { get; private set; }
    public string Value { get; private set; }

    public McApiPacketType PacketType => McApiPacketType.OnEntityWorldIdUpdated;

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(Id);
        writer.Put(Value, Constants.MaxStringLength);
    }

    public void Deserialize(NetDataReader reader)
    {
        Id = reader.GetInt();
        Value = reader.GetString(Constants.MaxStringLength);
    }

    public McApiOnEntityWorldIdUpdatedPacket Set(int id = 0, string value = "")
    {
        Id = id;
        Value = value;
        return this;
    }
}