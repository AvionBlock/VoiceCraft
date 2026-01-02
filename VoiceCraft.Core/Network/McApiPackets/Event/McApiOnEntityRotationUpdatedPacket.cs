using System.Numerics;
using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.McApiPackets.Event
{
    public class McApiOnEntityRotationUpdatedPacket : IMcApiPacket
    {
        public McApiOnEntityRotationUpdatedPacket() : this(0, Vector2.Zero)
        {
        }

        public McApiOnEntityRotationUpdatedPacket(int id, Vector2 value)
        {
            Id = id;
            Value = value;
        }

        public McApiPacketType PacketType => McApiPacketType.OnEntityRotationUpdated;

        public int Id { get; private set; }
        public Vector2 Value { get; private set; }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(Id);
            writer.Put(Value.X);
            writer.Put(Value.Y);
        }

        public void Deserialize(NetDataReader reader)
        {
            Id = reader.GetInt();
            Value = new Vector2(reader.GetFloat(), reader.GetFloat());
        }

        public McApiOnEntityRotationUpdatedPacket Set(int id = 0, Vector2 value = new Vector2())
        {
            Id = id;
            Value = value;
            return this;
        }
    }
}