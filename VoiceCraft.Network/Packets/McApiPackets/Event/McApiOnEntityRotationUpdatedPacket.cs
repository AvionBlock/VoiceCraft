using System.Numerics;
using LiteNetLib.Utils;

namespace VoiceCraft.Network.Packets.McApiPackets.Event;

public class McApiOnEntityRotationUpdatedPacket(int id, Vector2 value) : IMcApiEventPacket
{
    public McApiOnEntityRotationUpdatedPacket() : this(0, Vector2.Zero)
    {
    }

    public EventType EventType => EventType.OnEntityRotationUpdated;
    public int Id { get; private set; } = id;
    public Vector2 Value { get; private set; } = value;

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(Id);
        writer.Put(Value.X);
        writer.Put(Value.Y);
    }

    public void Deserialize(NetDataReader reader)
    {
        Id = reader.GetInt();
        Value = new Vector2(reader.GetFloat(), reader.GetFloat());
    }
    
    public void Return()
    {
        PacketPool<McApiOnEntityRotationUpdatedPacket>.Return(this);
    }

    public void Set(int id = 0, Vector2 value = new())
    {
        Id = id;
        Value = value;
    }
}