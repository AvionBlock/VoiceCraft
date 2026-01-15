using LiteNetLib.Utils;
using VoiceCraft.Core;

namespace VoiceCraft.Network.Packets.McApiPackets.Request;

public class McApiSetEntityEffectBitmaskRequestPacket : IMcApiPacket
{
    public McApiSetEntityEffectBitmaskRequestPacket() : this(0, 0)
    {
    }

    public McApiSetEntityEffectBitmaskRequestPacket(int id, ushort value)
    {
        Id = id;
        Value = value;
    }

    public int Id { get; private set; }
    public ushort Value { get; private set; }

    public McApiPacketType PacketType => McApiPacketType.SetEntityEffectBitmaskRequest;

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(Id);
        writer.Put(Value);
    }

    public void Deserialize(NetDataReader reader)
    {
        Id = reader.GetInt();
        Value = reader.GetUShort();
    }

    public McApiSetEntityEffectBitmaskRequestPacket Set(int id = 0, ushort value = 0)
    {
        Id = id;
        Value = value;
        return this;
    }
}