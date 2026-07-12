using LiteNetLib.Utils;
using VoiceCraft.Core;

namespace VoiceCraft.Network.Packets.VcPackets.Request;

public class VcSetWorldIdRequestPacket(string value) : IVoiceCraftPacket
{
    public VcSetWorldIdRequestPacket() : this(string.Empty)
    {
    }

    public string Value { get; private set; } = value;

    public VcPacketType PacketType => VcPacketType.SetWorldIdRequest;

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(Value, Constants.MaxDescriptionStringLength);
    }

    public void Deserialize(NetDataReader reader)
    {
        Value = reader.GetString(Constants.MaxDescriptionStringLength);
    }
    
    public void Return()
    {
        PacketPool<VcSetWorldIdRequestPacket>.Return(this);
    }

    public void Set(string value = "")
    {
        Value = value;
    }
}