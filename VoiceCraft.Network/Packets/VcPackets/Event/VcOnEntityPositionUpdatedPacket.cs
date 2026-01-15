using System.Numerics;
using LiteNetLib.Utils;
using VoiceCraft.Core;

namespace VoiceCraft.Network.Packets.VcPackets.Event;

public class VcOnEntityPositionUpdatedPacket : IVoiceCraftPacket
{
    public VcOnEntityPositionUpdatedPacket() : this(0, Vector3.Zero)
    {
    }

    public VcOnEntityPositionUpdatedPacket(int id, Vector3 value)
    {
        Id = id;
        Value = value;
    }

    public int Id { get; private set; }
    public Vector3 Value { get; private set; }

    public VcPacketType PacketType => VcPacketType.OnEntityPositionUpdated;

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(Id);
        writer.Put(Value.X);
        writer.Put(Value.Y);
        writer.Put(Value.Z);
    }

    public void Deserialize(NetDataReader reader)
    {
        Id = reader.GetInt();
        Value = new Vector3(reader.GetFloat(), reader.GetFloat(), reader.GetFloat());
    }

    public VcOnEntityPositionUpdatedPacket Set(int id = 0, Vector3 value = new())
    {
        Id = id;
        Value = value;
        return this;
    }
}