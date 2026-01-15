using LiteNetLib.Utils;
using VoiceCraft.Core;

namespace VoiceCraft.Network.Packets.VcPackets.Request;

public class VcSetNameRequestPacket : IVoiceCraftPacket
{
    public VcSetNameRequestPacket() : this(string.Empty)
    {
    }

    public VcSetNameRequestPacket(string value)
    {
        Value = value;
    }

    public string Value { get; private set; }

    public VcPacketType PacketType => VcPacketType.SetNameRequest;

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(Value, Constants.MaxStringLength);
    }

    public void Deserialize(NetDataReader reader)
    {
        Value = reader.GetString(Constants.MaxStringLength);
    }

    public VcSetNameRequestPacket Set(string value = "")
    {
        Value = value;
        return this;
    }
}