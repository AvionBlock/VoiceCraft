using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.Packets
{
    public class SetIdPacket : VoiceCraftPacket
    {
        public SetIdPacket(int id = 0)
        {
            Id = id;
        }

        public override PacketType PacketType => PacketType.SetId;

        public int Id { get; private set; }

        public override void Serialize(NetDataWriter writer)
        {
            writer.Put(Id);
        }

        public override void Deserialize(NetDataReader reader)
        {
            Id = reader.GetInt();
        }
    }
}