using LiteNetLib.Utils;
using VoiceCraft.Core;

namespace VoiceCraft.Network.Packets.VcPackets.Request;

public class VcSetNameRequestPacket(string value) : IVoiceCraftPacket
{
    public VcSetNameRequestPacket() : this(string.Empty)
    {
    }

    public string Value { get; private set; } = value;

    public VcPacketType PacketType => VcPacketType.SetNameRequest;

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
        PacketPool<VcSetNameRequestPacket>.Return(this);
    }

    public void Set(string value = "")
    {
        Value = value;
    }
}