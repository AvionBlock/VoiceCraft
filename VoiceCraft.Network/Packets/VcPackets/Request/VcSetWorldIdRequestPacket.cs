using LiteNetLib.Utils;
using VoiceCraft.Core;

namespace VoiceCraft.Network.Packets.VcPackets.Request;

public class VcSetWorldIdRequestPacket : IVoiceCraftPacket
{
    public VcSetWorldIdRequestPacket() : this(string.Empty)
    {
    }

    public VcSetWorldIdRequestPacket(string value)
    {
        Value = value;
    }

    public string Value { get; private set; }

    public VcPacketType PacketType => VcPacketType.SetWorldIdRequest;

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(Value, Constants.MaxDescriptionStringLength);
    }

    public void Deserialize(NetDataReader reader)
    {
        Value = reader.GetString(Constants.MaxDescriptionStringLength);
    }

    public VcSetWorldIdRequestPacket Set(string value = "")
    {
        Value = value;
        return this;
    }
}