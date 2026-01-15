using LiteNetLib.Utils;
using VoiceCraft.Core;

namespace VoiceCraft.Network.Packets.VcPackets.Request;

public class VcSetCaveFactorRequest : IVoiceCraftPacket
{
    public VcSetCaveFactorRequest() : this(0.0f)
    {
    }

    public VcSetCaveFactorRequest(float value)
    {
        Value = value;
    }

    public float Value { get; private set; }

    public VcPacketType PacketType => VcPacketType.SetCaveFactorRequest;

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(Value);
    }

    public void Deserialize(NetDataReader reader)
    {
        Value = reader.GetFloat();
    }

    public VcSetCaveFactorRequest Set(float value = 0.0f)
    {
        Value = value;
        return this;
    }
}