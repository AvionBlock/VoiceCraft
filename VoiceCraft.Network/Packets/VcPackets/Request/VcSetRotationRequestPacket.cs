using System.Numerics;
using LiteNetLib.Utils;

namespace VoiceCraft.Network.Packets.VcPackets.Request;

public class VcSetRotationRequestPacket(Vector2 value) : IVoiceCraftPacket
{
    public VcSetRotationRequestPacket() : this(Vector2.Zero)
    {
    }

    public Vector2 Value { get; private set; } = value;

    public VcPacketType PacketType => VcPacketType.SetRotationRequest;

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(Value.X);
        writer.Put(Value.Y);
    }

    public void Deserialize(NetDataReader reader)
    {
        Value = new Vector2(reader.GetFloat(), reader.GetFloat());
    }

    public VcSetRotationRequestPacket Set(Vector2 value = new())
    {
        Value = value;
        return this;
    }
}