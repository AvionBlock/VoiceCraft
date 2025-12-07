using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.McApiPackets.Event
{
    public class McApiOnEntityDestroyedPacket : IMcApiPacket
    {
        public McApiOnEntityDestroyedPacket() : this(0)
        {
        }

        public McApiOnEntityDestroyedPacket(int id)
        {
            Id = id;
        }

        public McApiPacketType PacketType => McApiPacketType.OnEntityDestroyed;

        public int Id { get; private set; }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(Id);
        }

        public void Deserialize(NetDataReader reader)
        {
            Id = reader.GetInt();
        }

        public McApiOnEntityDestroyedPacket Set(int id = 0)
        {
            Id = id;
            return this;
        }
    }
}