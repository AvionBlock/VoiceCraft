using LiteNetLib.Utils;
using VoiceCraft.Core;

namespace VoiceCraft.Network.Packets.VcPackets.Request;

public class VcSetEffectBitmaskRequestPacket : IVoiceCraftPacket
{
    public VcSetEffectBitmaskRequestPacket() : this(0)
    {
    }

    public VcSetEffectBitmaskRequestPacket(ushort value)
    {
        Value = value;
    }

    public ushort Value { get; private set; }

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