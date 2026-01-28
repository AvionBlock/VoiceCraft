using LiteNetLib.Utils;

namespace VoiceCraft.Network.Packets.VcPackets.Request;

public class VcSetListenBitmaskRequestPacket(ushort value) : IVoiceCraftPacket
{
    public VcSetListenBitmaskRequestPacket() : this(0)
    {
    }

    public ushort Value { get; private set; } = value;

    public VcPacketType PacketType => VcPacketType.SetListenBitmaskRequest;

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(Value);
    }

    public void Deserialize(NetDataReader reader)
    {
        Value = reader.GetUShort();
    }

    public VcSetListenBitmaskRequestPacket Set(ushort value = 0)
    {
        Value = value;
        return this;
    }
}