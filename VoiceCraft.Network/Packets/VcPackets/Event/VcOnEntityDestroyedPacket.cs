using LiteNetLib.Utils;
using VoiceCraft.Core;

namespace VoiceCraft.Network.Packets.VcPackets.Event;

public class VcOnEntityDestroyedPacket : IVoiceCraftPacket
{
    public VcOnEntityDestroyedPacket() : this(0)
    {
    }

    public VcOnEntityDestroyedPacket(int id)
    {
        Id = id;
    }

    public int Id { get; private set; }

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