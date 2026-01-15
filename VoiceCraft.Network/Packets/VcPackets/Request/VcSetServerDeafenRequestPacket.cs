using LiteNetLib.Utils;
using VoiceCraft.Core;

namespace VoiceCraft.Network.Packets.VcPackets.Request;

public class VcSetServerDeafenRequestPacket : IVoiceCraftPacket
{
    public VcSetServerDeafenRequestPacket() : this(false)
    {
    }

    public VcSetServerDeafenRequestPacket(bool value)
    {
        Value = value;
    }

    public bool Value { get; private set; }

    public VcPacketType PacketType => VcPacketType.SetServerDeafenRequest;

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(Value);
    }

    public void Deserialize(NetDataReader reader)
    {
        Value = reader.GetBool();
    }

    public VcSetServerDeafenRequestPacket Set(bool value = false)
    {
        Value = value;
        return this;
    }
}