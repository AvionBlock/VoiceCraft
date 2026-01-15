using LiteNetLib.Utils;
using VoiceCraft.Core;

namespace VoiceCraft.Network.Packets.VcPackets.Request;

public class VcSetTitleRequestPacket : IVoiceCraftPacket
{
    public VcSetTitleRequestPacket() : this(string.Empty)
    {
    }

    public VcSetTitleRequestPacket(string value)
    {
        Value = value;
    }

    public string Value { get; private set; }

    public VcPacketType PacketType => VcPacketType.SetTitleRequest;

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(Value, Constants.MaxDescriptionStringLength);
    }

    public void Deserialize(NetDataReader reader)
    {
        Value = reader.GetString(Constants.MaxDescriptionStringLength);
    }

    public VcSetTitleRequestPacket Set(string value = "")
    {
        Value = value;
        return this;
    }
}