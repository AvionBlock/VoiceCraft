using System.Numerics;
using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.Packets
{
    public class SetRotationPacket : VoiceCraftPacket
    {
        public SetRotationPacket(int id = 0, Quaternion value = new Quaternion())
        {
            Id = id;
            Value = value;
        }

        public override PacketType PacketType => PacketType.SetRotation;

        public int Id { get; private set; }
        public Quaternion Value { get; private set; }

        public override void Serialize(NetDataWriter writer)
        {
            writer.Put(Id);
            writer.Put(Value.X);
            writer.Put(Value.Y);
            writer.Put(Value.Z);
            writer.Put(Value.W);
        }

        public override void Deserialize(NetDataReader reader)
        {
            Id = reader.GetInt();
            var x = reader.GetFloat();
            var y = reader.GetFloat();
            var z = reader.GetFloat();
            var w = reader.GetFloat();
            Value = new Quaternion(x, y, z, w);
        }
    }
}