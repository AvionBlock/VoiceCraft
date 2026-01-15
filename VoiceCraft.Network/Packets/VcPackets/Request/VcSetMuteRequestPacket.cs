using LiteNetLib.Utils;
using VoiceCraft.Core;

namespace VoiceCraft.Network.Packets.VcPackets.Request;

public class VcSetMuteRequestPacket : IVoiceCraftPacket
{
    public VcSetMuteRequestPacket() : this(false)
    {
    }

    public VcSetMuteRequestPacket(bool value)
    {
        Value = value;
    }

    public bool Value { get; private set; }

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