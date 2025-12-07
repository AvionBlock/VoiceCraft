using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.McApiPackets.Event
{
    public class McApiOnEntityDestroyedPacket : McApiPacket
    {
        public McApiOnEntityDestroyedPacket(int id = 0)
        {
            Id = id;
        }

        public override McApiPacketType PacketType => McApiPacketType.OnEntityDestroyed;

        public int Id { get; private set; }

        public override void Serialize(NetDataWriter writer)
        {
            writer.Put(Id);
        }

        public override void Deserialize(NetDataReader reader)
        {
            Id = reader.GetInt();
        }
    }
}