using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.Packets
{
    public class SetVisibilityPacket : VoiceCraftPacket
    {
        public override PacketType PacketType => PacketType.SetVisibility;
        
        public int Id { get; private set; }
        public bool Value { get; private set; }

        public SetVisibilityPacket(int id = 0, bool value = false)
        {
            Id = id;
            Value = value;
        }
        
        public override void Serialize(NetDataWriter writer)
        {
            writer.Put(Id);
            writer.Put(Value);
        }

        public override void Deserialize(NetDataReader reader)
        {
            Id = reader.GetInt();
            Value = reader.GetBool();
        }
    }
}