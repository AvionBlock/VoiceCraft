using LiteNetLib.Utils;

namespace VoiceCraft.Network.Packets.McApiPackets.Request;

public class McApiSetEntityListenBitmaskRequestPacket(int id, ushort value) : IMcApiPacket
{
    public McApiSetEntityListenBitmaskRequestPacket() : this(0, 0)
    {
    }

    public int Id { get; private set; } = id;
    public ushort Value { get; private set; } = value;

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