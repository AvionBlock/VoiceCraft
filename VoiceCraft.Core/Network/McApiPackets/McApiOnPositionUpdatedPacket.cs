using System.Numerics;
using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.McApiPackets
{
    public class McApiOnPositionUpdatedPacket : McApiPacket
    {
        public McApiOnPositionUpdatedPacket(int id = 0, Vector3 value = new Vector3())
        {
            Id = id;
            Value = value;
        }

        public override McApiPacketType PacketType => McApiPacketType.OnEntityPositionUpdated;

        public int Id { get; private set; }
        public Vector3 Value { get; private set; }

        public override void Serialize(NetDataWriter writer)
        {
            writer.Put(Id);
            writer.Put(Value.X);
            writer.Put(Value.Y);
            writer.Put(Value.Z);
        }

        public override void Deserialize(NetDataReader reader)
        {
            Id = reader.GetInt();
            Value = new Vector3(reader.GetFloat(), reader.GetFloat(), reader.GetFloat());
        }
    }
}