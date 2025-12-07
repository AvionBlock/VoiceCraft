using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.VcPackets.Event
{
    public class VcOnEntityDestroyedPacket : IVoiceCraftPacket
    {
        public VcOnEntityDestroyedPacket(int id = 0)
        {
            Id = id;
        }

        public VcPacketType PacketType => VcPacketType.OnEntityDestroyed;

        public int Id { get; private set; }

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
}