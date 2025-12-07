using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.McApiPackets.Event
{
    public class McApiOnEntityCaveFactorUpdatedPacket : IMcApiPacket
    {
        public McApiOnEntityCaveFactorUpdatedPacket() : this(0, 0.0f)
        {
        }

        public McApiOnEntityCaveFactorUpdatedPacket(int id, float value)
        {
            Id = id;
            Value = value;
        }

        public McApiPacketType PacketType => McApiPacketType.OnEntityCaveFactorUpdated;

        public int Id { get; private set; }
        public float Value { get; private set; }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(Id);
            writer.Put(Value);
        }

        public void Deserialize(NetDataReader reader)
        {
            Id = reader.GetInt();
            Value = reader.GetFloat();
        }

        public McApiOnEntityCaveFactorUpdatedPacket Set(int id = 0, float value = 0)
        {
            Id = id;
            Value = value;
            return this;
        }
    }
}