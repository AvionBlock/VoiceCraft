using LiteNetLib.Utils;

namespace VoiceCraft.Network.Packets.VcPackets.Request;

public class VcSetDeafenRequestPacket(bool value) : IVoiceCraftPacket
{
    public VcSetDeafenRequestPacket() : this(false)
    {
    }

    public bool Value { get; private set; } = value;

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