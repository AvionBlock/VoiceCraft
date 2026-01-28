using LiteNetLib.Utils;
using VoiceCraft.Core;

namespace VoiceCraft.Network.Packets.McApiPackets.Request;

public class McApiSetEntityTitleRequestPacket(int id, string value) : IMcApiPacket
{
    public McApiSetEntityTitleRequestPacket() : this(0, string.Empty)
    {
    }

    public int Id { get; private set; } = id;
    public string Value { get; private set; } = value;

    public McApiPacketType PacketType => McApiPacketType.SetEntityTitleRequest;

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

    public McApiSetEntityTitleRequestPacket Set(int id = 0, string value = "")
    {
        Id = id;
        Value = value;
        return this;
    }
}