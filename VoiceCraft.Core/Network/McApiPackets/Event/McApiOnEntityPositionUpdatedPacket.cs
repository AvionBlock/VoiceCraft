using System.Numerics;
using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.McApiPackets.Event
{
    public class McApiOnEntityPositionUpdatedPacket : IMcApiPacket
    {
        public McApiOnEntityPositionUpdatedPacket() : this(0, Vector3.Zero)
        {
        }

        public McApiOnEntityPositionUpdatedPacket(int id, Vector3 value)
        {
            Id = id;
            Value = value;
        }

        public McApiPacketType PacketType => McApiPacketType.OnEntityPositionUpdated;

        public int Id { get; private set; }
        public Vector3 Value { get; private set; }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(Id);
            writer.Put(Value.X);
            writer.Put(Value.Y);
            writer.Put(Value.Z);
        }

        public void Deserialize(NetDataReader reader)
        {
            Id = reader.GetInt();
            Value = new Vector3(reader.GetFloat(), reader.GetFloat(), reader.GetFloat());
        }

        public McApiOnEntityPositionUpdatedPacket Set(int id = 0, Vector3 value = new Vector3())
        {
            Id = id;
            Value = value;
            return this;
        }
    }
}