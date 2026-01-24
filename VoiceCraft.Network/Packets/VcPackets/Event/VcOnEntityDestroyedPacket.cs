using LiteNetLib.Utils;

namespace VoiceCraft.Network.Packets.VcPackets.Event;

public class VcOnEntityDestroyedPacket(int id) : IVoiceCraftPacket
{
    public VcOnEntityDestroyedPacket() : this(0)
    {
    }

    public int Id { get; private set; } = id;

    public VcPacketType PacketType => VcPacketType.OnEntityDestroyed;

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(Id);
    }

    public void Deserialize(NetDataReader reader)
    {
        Id = reader.GetInt();
    }

    public VcOnEntityDestroyedPacket Set(int id = 0)
    {
        Id = id;
        return this;
    }
}