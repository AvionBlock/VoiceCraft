using LiteNetLib.Utils;

namespace VoiceCraft.Network.Packets.VcPackets.Request;

public class VcSetServerDeafenRequestPacket(bool value) : IVoiceCraftPacket
{
    public VcSetServerDeafenRequestPacket() : this(false)
    {
    }

    public bool Value { get; private set; } = value;

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