using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.Packets
{
    public class EntityCreatedPacket : VoiceCraftPacket
    {
        public override PacketType PacketType => PacketType.EntityCreated;
        public int NetworkId { get; set; }
        public ulong Bitmask { get; set; }
        public string Name { get; set; } = string.Empty;

        public override void Serialize(NetDataWriter writer)
        {
            writer.Put(NetworkId);
            writer.Put(Bitmask);
            writer.Put(Name);
        }

        public override void Deserialize(NetDataReader reader)
        {
            NetworkId = reader.GetInt();
            Bitmask = reader.GetULong();
            Name = reader.GetString();
        }
    }
}