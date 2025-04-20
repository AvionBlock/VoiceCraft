using System.Numerics;
using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.Packets
{
    public class SetRotationPacket : VoiceCraftPacket
    {
        public override PacketType PacketType => PacketType.SetRotation;
        public byte Id { get; private set; }
        public Quaternion Rotation { get; private set; }

        public SetRotationPacket(byte id = 0, Quaternion rotation = new Quaternion())
        {
            Id = id;
            Rotation = rotation;
        }
        
        public override void Serialize(NetDataWriter writer)
        {
            writer.Put(Id);
            writer.Put(Rotation.X);
            writer.Put(Rotation.Y);
            writer.Put(Rotation.Z);
            writer.Put(Rotation.W);
        }

        public override void Deserialize(NetDataReader reader)
        {
            Id = reader.GetByte();
            var x = reader.GetFloat();
            var y = reader.GetFloat();
            var z = reader.GetFloat();
            var w = reader.GetFloat();
            Rotation = new Quaternion(x, y, z, w);
        }
    }
}