using LiteNetLib.Utils;
using VoiceCraft.Core;

namespace VoiceCraft.Network.Packets.McApiPackets.Request;

public class McApiSetEntityNameRequestPacket(int id, string value) : IMcApiPacket
{
    public McApiSetEntityNameRequestPacket() : this(0, string.Empty)
    {
    }

    public int Id { get; private set; } = id;
    public string Value { get; private set; } = value;

    public McApiPacketType PacketType => McApiPacketType.SetEntityNameRequest;

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(Id);
        writer.Put(Value, Constants.MaxStringLength);
    }

    public void Deserialize(NetDataReader reader)
    {
        Id = reader.GetInt();
        Value = reader.GetString(Constants.MaxStringLength);
    }

    public McApiSetEntityNameRequestPacket Set(int id = 0, string value = "")
    {
        Id = id;
        Value = value;
        return this;
    }
}