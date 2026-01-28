using LiteNetLib.Utils;

namespace VoiceCraft.Network.Packets.McApiPackets.Request;

public class McApiSetEntityCaveFactorRequestPacket(int id, float value) : IMcApiPacket
{
    public McApiSetEntityCaveFactorRequestPacket() : this(0, 0.0f)
    {
    }

    public int Id { get; private set; } = id;
    public float Value { get; private set; } = value;

    public McApiPacketType PacketType => McApiPacketType.SetEntityCaveFactorRequest;

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(Id);
        writer.Put(Value);
    }

    public void Deserialize(NetDataReader reader)
    {
        Id = reader.GetInt();
        Value = reader.GetFloat();
    }

    public McApiSetEntityCaveFactorRequestPacket Set(int id = 0, float value = 0.0f)
    {
        Id = id;
        Value = value;
        return this;
    }
}