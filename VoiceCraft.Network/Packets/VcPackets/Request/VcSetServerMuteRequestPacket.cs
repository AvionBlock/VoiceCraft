using LiteNetLib.Utils;
using VoiceCraft.Core;

namespace VoiceCraft.Network.Packets.VcPackets.Request;

public class VcSetServerMuteRequestPacket : IVoiceCraftPacket
{
    public VcSetServerMuteRequestPacket() : this(false)
    {
    }

    public VcSetServerMuteRequestPacket(bool value)
    {
        Value = value;
    }

    public bool Value { get; private set; }

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