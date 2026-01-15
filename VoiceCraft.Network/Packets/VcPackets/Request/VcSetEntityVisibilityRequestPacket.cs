using LiteNetLib.Utils;
using VoiceCraft.Core;

namespace VoiceCraft.Network.Packets.VcPackets.Request;

public class VcSetEntityVisibilityRequestPacket : IVoiceCraftPacket
{
    public VcSetEntityVisibilityRequestPacket() : this(0, false)
    {
    }

    public VcSetEntityVisibilityRequestPacket(int id, bool value)
    {
        Id = id;
        Value = value;
    }

    public int Id { get; private set; }
    public bool Value { get; private set; }

    public VcPacketType PacketType => VcPacketType.SetEntityVisibilityRequest;

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(Id);
        writer.Put(Value);
    }

    public void Deserialize(NetDataReader reader)
    {
        Id = reader.GetInt();
        Value = reader.GetBool();
    }

    public VcSetEntityVisibilityRequestPacket Set(int id = 0, bool value = false)
    {
        Id = id;
        Value = value;
        return this;
    }
}