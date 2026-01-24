using LiteNetLib.Utils;

namespace VoiceCraft.Network.Packets.VcPackets.Request;

public class VcSetTalkBitmaskRequestPacket(ushort value) : IVoiceCraftPacket
{
    public VcSetTalkBitmaskRequestPacket() : this(0)
    {
    }

    public ushort Value { get; private set; } = value;

    public VcPacketType PacketType => VcPacketType.SetTalkBitmaskRequest;

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(Value);
    }

    public void Deserialize(NetDataReader reader)
    {
        Value = reader.GetUShort();
    }

    public VcSetTalkBitmaskRequestPacket Set(ushort value = 0)
    {
        Value = value;
        return this;
    }
}