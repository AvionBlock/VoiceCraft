using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.Packets
{
    public class SetVisibilityPacket : VoiceCraftPacket
    {
        public override PacketType PacketType => PacketType.SetVisibility;
        public int Id { get; private set; }
        public bool Visibility { get; private set; }

        public SetVisibilityPacket(int id = 0, bool visibility = false)
        {
            Id = id;
            Visibility = visibility;
        }
        
        public override void Serialize(NetDataWriter writer)
        {
            writer.Put(Id);
            writer.Put(Visibility);
        }

        public override void Deserialize(NetDataReader reader)
        {
            Id = reader.GetInt();
            Visibility = reader.GetBool();
        }
    }
}