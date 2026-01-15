using LiteNetLib.Utils;
using VoiceCraft.Core;

namespace VoiceCraft.Network.Packets.McApiPackets.Request;

public class McApiSetEntityListenBitmaskRequestPacket : IMcApiPacket
{
    public McApiSetEntityListenBitmaskRequestPacket() : this(0, 0)
    {
    }

    public McApiSetEntityListenBitmaskRequestPacket(int id, ushort value)
    {
        Id = id;
        Value = value;
    }

    public int Id { get; private set; }
    public ushort Value { get; private set; }

    public McApiPacketType PacketType => McApiPacketType.SetEntityListenBitmaskRequest;

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

    public McApiSetEntityListenBitmaskRequestPacket Set(int id = 0, ushort value = 0)
    {
        Id = id;
        Value = value;
        return this;
    }
}