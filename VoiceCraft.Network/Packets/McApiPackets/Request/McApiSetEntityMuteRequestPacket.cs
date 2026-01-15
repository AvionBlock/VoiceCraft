using LiteNetLib.Utils;
using VoiceCraft.Core;

namespace VoiceCraft.Network.Packets.McApiPackets.Request;

public class McApiSetEntityMuteRequestPacket : IMcApiPacket
{
    public McApiSetEntityMuteRequestPacket() : this(0, false)
    {
    }

    public McApiSetEntityMuteRequestPacket(int id, bool value)
    {
        Id = id;
        Value = value;
    }

    public int Id { get; private set; }
    public bool Value { get; private set; }

    public McApiPacketType PacketType => McApiPacketType.SetEntityMuteRequest;

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

    public McApiSetEntityMuteRequestPacket Set(int id = 0, bool value = false)
    {
        Id = id;
        Value = value;
        return this;
    }
}