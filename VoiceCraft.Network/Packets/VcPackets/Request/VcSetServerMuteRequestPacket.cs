using LiteNetLib.Utils;

namespace VoiceCraft.Network.Packets.VcPackets.Request;

public class VcSetServerMuteRequestPacket(bool value) : IVoiceCraftPacket
{
    public VcSetServerMuteRequestPacket() : this(false)
    {
    }

    public bool Value { get; private set; } = value;

    public VcPacketType PacketType => VcPacketType.SetServerMuteRequest;

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(Value);
    }

    public void Deserialize(NetDataReader reader)
    {
        Value = reader.GetBool();
    }

    public VcSetServerMuteRequestPacket Set(bool value = false)
    {
        Value = value;
        return this;
    }
}