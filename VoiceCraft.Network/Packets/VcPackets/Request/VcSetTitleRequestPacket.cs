using LiteNetLib.Utils;
using VoiceCraft.Core;

namespace VoiceCraft.Network.Packets.VcPackets.Request;

public class VcSetTitleRequestPacket(string value) : IVoiceCraftPacket
{
    public VcSetTitleRequestPacket() : this(string.Empty)
    {
    }

    public string Value { get; private set; } = value;

    public VcPacketType PacketType => VcPacketType.SetTitleRequest;

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(Value, Constants.MaxStringLength);
    }

    public void Deserialize(NetDataReader reader)
    {
        Value = reader.GetString(Constants.MaxStringLength);
    }
    
    public void Return()
    {
        PacketPool<VcSetTitleRequestPacket>.Return(this);
    }

    public void Set(string value = "")
    {
        Value = value;
    }
}
