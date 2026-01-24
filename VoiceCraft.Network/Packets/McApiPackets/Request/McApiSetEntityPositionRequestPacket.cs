using System.Numerics;
using LiteNetLib.Utils;

namespace VoiceCraft.Network.Packets.McApiPackets.Request;

public class McApiSetEntityPositionRequestPacket(int id, Vector3 value) : IMcApiPacket
{
    public McApiSetEntityPositionRequestPacket() : this(0, Vector3.Zero)
    {
    }

    public int Id { get; private set; } = id;
    public Vector3 Value { get; private set; } = value;

    public McApiPacketType PacketType => McApiPacketType.SetEntityPositionRequest;

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(Id);
        writer.Put(Value.X);
        writer.Put(Value.Y);
        writer.Put(Value.Z);
    }

    public void Deserialize(NetDataReader reader)
    {
        Id = reader.GetInt();
        Value = new Vector3(reader.GetFloat(), reader.GetFloat(), reader.GetFloat());
    }

    public McApiSetEntityPositionRequestPacket Set(int id = 0, Vector3 value = new())
    {
        Id = id;
        Value = value;
        return this;
    }
}