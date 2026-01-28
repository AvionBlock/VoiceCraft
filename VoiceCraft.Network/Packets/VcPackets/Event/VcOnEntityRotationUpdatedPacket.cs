using System.Numerics;
using LiteNetLib.Utils;

namespace VoiceCraft.Network.Packets.VcPackets.Event;

public class VcOnEntityRotationUpdatedPacket(int id, Vector2 value) : IVoiceCraftPacket
{
    public VcOnEntityRotationUpdatedPacket() : this(0, Vector2.Zero)
    {
    }

    public int Id { get; private set; } = id;
    public Vector2 Value { get; private set; } = value;

    public VcPacketType PacketType => VcPacketType.OnEntityRotationUpdated;

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

    public VcOnEntityRotationUpdatedPacket Set(int id = 0, Vector2 value = new())
    {
        Id = id;
        Value = value;
        return this;
    }
}