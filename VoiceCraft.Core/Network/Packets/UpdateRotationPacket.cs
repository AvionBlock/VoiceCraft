using System.Numerics;
using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.Packets
{
    public class UpdateRotationPacket : VoiceCraftPacket
    {
        public override PacketType PacketType => PacketType.UpdateRotation;
        public int NetworkId { get; set; }
        public Quaternion Rotation { get; set; }

        public UpdateRotationPacket(int networkId, Quaternion rotation)
        {
            NetworkId = networkId;
            Rotation = rotation;
        }
        
        public override void Serialize(NetDataWriter writer)
        {
            writer.Put(NetworkId);
            writer.Put(Rotation.X);
            writer.Put(Rotation.Y);
            writer.Put(Rotation.Z);
            writer.Put(Rotation.W);
        }

        public override void Deserialize(NetDataReader reader)
        {
            NetworkId = reader.GetInt();
            var x = reader.GetFloat();
            var y = reader.GetFloat();
            var z = reader.GetFloat();
            var w = reader.GetFloat();
            Rotation = new Quaternion(x, y, z, w);
        }
    }
}