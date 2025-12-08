using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.McApiPackets.Event
{
    public class McApiOnEntityVisibilityUpdatedPacket : IMcApiPacket
    {
        public McApiOnEntityVisibilityUpdatedPacket() : this(0, 0, false)
        {
        }

        public McApiOnEntityVisibilityUpdatedPacket(int id, int id2, bool value)
        {
            Id = id;
            Id2 = id2;
            Value = value;
        }

        public McApiPacketType PacketType => McApiPacketType.OnEntityVisibilityUpdated;

        public int Id { get; private set; }
        public int Id2 { get; private set; }
        public bool Value { get; private set; }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(Id);
            writer.Put(Id2);
            writer.Put(Value);
        }

        public void Deserialize(NetDataReader reader)
        {
            Id = reader.GetInt();
            Id2 = reader.GetInt();
            Value = reader.GetBool();
        }

        public McApiOnEntityVisibilityUpdatedPacket Set(int id = 0, int id2 = 0, bool value = false)
        {
            Id = id;
            Id2 = id2;
            Value = value;
            return this;
        }
    }
}