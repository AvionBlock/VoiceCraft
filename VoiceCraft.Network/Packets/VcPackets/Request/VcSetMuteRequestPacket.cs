using LiteNetLib.Utils;

namespace VoiceCraft.Network.Packets.VcPackets.Request;

public class VcSetMuteRequestPacket(bool value) : IVoiceCraftPacket
{
    public VcSetMuteRequestPacket() : this(false)
    {
    }

    public bool Value { get; private set; } = value;

    public VcPacketType PacketType => VcPacketType.SetMuteRequest;

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(Value);
    }

    public void Deserialize(NetDataReader reader)
    {
        Value = reader.GetBool();
    }

    public VcSetMuteRequestPacket Set(bool value = false)
    {
        Value = value;
        return this;
    }
}