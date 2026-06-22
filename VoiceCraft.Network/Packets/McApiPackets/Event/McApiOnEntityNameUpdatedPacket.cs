using LiteNetLib.Utils;
using VoiceCraft.Core;

namespace VoiceCraft.Network.Packets.McApiPackets.Event;

public class McApiOnEntityNameUpdatedPacket(int id, string value) : IMcApiEventPacket
{
    public McApiOnEntityNameUpdatedPacket() : this(0, string.Empty)
    {
    }

    public EventType EventType => EventType.OnEntityNameUpdated;
    public int Id { get; private set; } = id;
    public string Value { get; private set; } = value;

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
    
    public void Return()
    {
        PacketPool<McApiOnEntityNameUpdatedPacket>.Return(this);
    }

    public McApiOnEntityNameUpdatedPacket Set(int id = 0, string value = "")
    {
        Id = id;
        Value = value;
        return this;
    }
}