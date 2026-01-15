using System.Numerics;
using LiteNetLib.Utils;
using VoiceCraft.Core;

namespace VoiceCraft.Network.Packets.VcPackets.Event;

public class VcOnEntityRotationUpdatedPacket : IVoiceCraftPacket
{
    public VcOnEntityRotationUpdatedPacket() : this(0, Vector2.Zero)
    {
    }

    public VcOnEntityRotationUpdatedPacket(int id, Vector2 value)
    {
        Id = id;
        Value = value;
    }

    public int Id { get; private set; }
    public Vector2 Value { get; private set; }

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