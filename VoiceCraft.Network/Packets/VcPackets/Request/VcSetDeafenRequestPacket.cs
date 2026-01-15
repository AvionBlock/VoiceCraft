using LiteNetLib.Utils;
using VoiceCraft.Core;

namespace VoiceCraft.Network.Packets.VcPackets.Request;

public class VcSetDeafenRequestPacket : IVoiceCraftPacket
{
    public VcSetDeafenRequestPacket() : this(false)
    {
    }

    public VcSetDeafenRequestPacket(bool value)
    {
        Value = value;
    }

    public bool Value { get; private set; }

    public VcPacketType PacketType => VcPacketType.SetDeafenRequest;

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(Value);
    }

    public void Deserialize(NetDataReader reader)
    {
        Value = reader.GetBool();
    }

    public VcSetDeafenRequestPacket Set(bool value = false)
    {
        Value = value;
        return this;
    }
}