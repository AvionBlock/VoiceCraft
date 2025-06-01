using System.Numerics;
using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.Packets
{
    public class SetPositionPacket : VoiceCraftPacket
    {
        public SetPositionPacket(int id = 0, Vector3 value = new Vector3())
        {
            Id = id;
            Value = value;
        }

        public override PacketType PacketType => PacketType.SetPosition;

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
            var x = reader.GetFloat();
            var y = reader.GetFloat();
            var z = reader.GetFloat();
            Value = new Vector3(x, y, z);
        }
    }
}