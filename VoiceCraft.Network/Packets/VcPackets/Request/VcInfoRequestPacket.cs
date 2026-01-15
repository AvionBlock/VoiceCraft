using LiteNetLib.Utils;
using VoiceCraft.Core;

namespace VoiceCraft.Network.Packets.VcPackets.Request;

public class VcInfoRequestPacket : IVoiceCraftPacket
{
    public VcInfoRequestPacket()
    {
    }

    public VcInfoRequestPacket(int tick = 0)
    {
        Tick = tick;
    }

    public int Tick { get; private set; }

    public VcPacketType PacketType => VcPacketType.InfoRequest;

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(Tick);
    }

    public void Deserialize(NetDataReader reader)
    {
        Tick = reader.GetInt();
    }

    public VcInfoRequestPacket Set(int tick = 0)
    {
        Tick = tick;
        return this;
    }
}