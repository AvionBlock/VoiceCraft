using LiteNetLib.Utils;

namespace VoiceCraft.Network.Packets.VcPackets.Request;

public class VcSetEffectBitmaskRequestPacket(ushort value) : IVoiceCraftPacket
{
    public VcSetEffectBitmaskRequestPacket() : this(0)
    {
    }

    public ushort Value { get; private set; } = value;

    public VcPacketType PacketType => VcPacketType.SetEffectBitmaskRequest;

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(Value);
    }

    public void Deserialize(NetDataReader reader)
    {
        Value = reader.GetUShort();
    }

    public VcSetEffectBitmaskRequestPacket Set(ushort value = 0)
    {
        Value = value;
        return this;
    }
}