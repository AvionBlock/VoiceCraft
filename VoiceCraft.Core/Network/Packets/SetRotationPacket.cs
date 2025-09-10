using System.Numerics;
using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.Packets
{
    public class SetRotationPacket : VoiceCraftPacket
    {
        public SetRotationPacket(int id = 0, Vector2 value = new Vector2())
        {
            Id = id;
            Value = value;
        }

        public override PacketType PacketType => PacketType.SetRotation;

        public int Id { get; private set; }
        public Vector2 Value { get; private set; }

        public override void Serialize(NetDataWriter writer)
        {
            writer.Put(Id);
            writer.Put(Value.X);
            writer.Put(Value.Y);
        }

        public override void Deserialize(NetDataReader reader)
        {
            Id = reader.GetInt();
            Value = new Vector2(reader.GetFloat(), reader.GetFloat());
        }
    }
}