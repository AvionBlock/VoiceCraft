using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.Packets
{
    public class RemovePropertyPacket : VoiceCraftPacket
    {
        public override PacketType PacketType => PacketType.RemoveProperty;
        public int Id { get; private set; }
        public PropertyKey Key { get; private set; }

        public RemovePropertyPacket(int id = 0, PropertyKey key = PropertyKey.Unknown)
        {
            Id = id;
            Key = key;
        }

        public override void Serialize(NetDataWriter writer)
        {
            writer.Put(Id);
            writer.Put((ushort)Key);
        }

        public override void Deserialize(NetDataReader reader)
        {
            Id = reader.GetInt();
            Key = (PropertyKey)reader.GetUShort();
        }
    }
}