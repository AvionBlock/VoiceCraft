using LiteNetLib.Utils;
using VoiceCraft.Core;

namespace VoiceCraft.Network.Packets.VcPackets.Request;

public class VcSetDescriptionRequestPacket : IVoiceCraftPacket
{
    public VcSetDescriptionRequestPacket() : this(string.Empty)
    {
    }

    public VcSetDescriptionRequestPacket(string value)
    {
        Value = value;
    }

    public string Value { get; private set; }

    public VcPacketType PacketType => VcPacketType.SetDescriptionRequest;

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(Value, Constants.MaxDescriptionStringLength);
    }

    public void Deserialize(NetDataReader reader)
    {
        Value = reader.GetString(Constants.MaxDescriptionStringLength);
    }

    public VcSetDescriptionRequestPacket Set(string value = "")
    {
        Value = value;
        return this;
    }
}