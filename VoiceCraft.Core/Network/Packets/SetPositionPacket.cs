using System.Numerics;
using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.Packets
{
    public class SetPositionPacket : VoiceCraftPacket
    {
        public override PacketType PacketType => PacketType.SetPosition;
        public int NetworkId { get; private set; }
        public Vector3 Position { get; private set; }

        public SetPositionPacket(int networkId = 0, Vector3 position = new Vector3())
        {
            NetworkId = networkId;
            Position = position;
        }
        
        public override void Serialize(NetDataWriter writer)
        {
            writer.Put(NetworkId);
            writer.Put(Position.X);
            writer.Put(Position.Y);
            writer.Put(Position.Z);
        }

        public override void Deserialize(NetDataReader reader)
        {
            NetworkId = reader.GetInt();
            var x = reader.GetFloat();
            var y = reader.GetFloat();
            var z = reader.GetFloat();
            Position = new Vector3(x, y, z);
        }
    }
}