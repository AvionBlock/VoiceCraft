using LiteNetLib.Utils;

namespace VoiceCraft.Network.Packets.VcPackets.Request;

public class VcInfoRequestPacket(int tick) : IVoiceCraftPacket
{
    public VcInfoRequestPacket() : this(0)
    {
    }

    public VcPacketType PacketType => VcPacketType.InfoRequest;
    public int Tick { get; private set; } = tick;

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(Tick);
    }

    public void Deserialize(NetDataReader reader)
    {
        Tick = reader.GetInt();
    }
    
    public void Return()
    {
        PacketPool<VcInfoRequestPacket>.Return(this);
    }

    public void Set(int tick = 0)
    {
        Tick = tick;
    }
}