using LiteNetLib.Utils;

namespace VoiceCraft.Network.Packets.VcPackets.Request;

public class VcSetCaveFactorRequest(float value) : IVoiceCraftPacket
{
    public VcSetCaveFactorRequest() : this(0.0f)
    {
    }

    public float Value { get; private set; } = value;

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