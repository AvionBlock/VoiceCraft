using LiteNetLib.Utils;

namespace VoiceCraft.Network.Packets.VcPackets.Request;

public class VcSetMuffleFactorRequest(float value) : IVoiceCraftPacket
{
    public VcSetMuffleFactorRequest() : this(0.0f)
    {
    }

    public float Value { get; private set; } = value;

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