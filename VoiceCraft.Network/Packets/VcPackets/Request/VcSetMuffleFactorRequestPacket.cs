using LiteNetLib.Utils;
using VoiceCraft.Core;

namespace VoiceCraft.Network.Packets.VcPackets.Request;

public class VcSetMuffleFactorRequest : IVoiceCraftPacket
{
    public VcSetMuffleFactorRequest() : this(0.0f)
    {
    }

    public VcSetMuffleFactorRequest(float value)
    {
        Value = value;
    }

    public float Value { get; private set; }

    public VcPacketType PacketType => VcPacketType.SetMuffleFactorRequest;

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(Value);
    }

    public void Deserialize(NetDataReader reader)
    {
        Value = reader.GetFloat();
    }

    public VcSetMuffleFactorRequest Set(float value = 0.0f)
    {
        Value = value;
        return this;
    }
}