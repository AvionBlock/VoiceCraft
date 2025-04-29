using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.Packets
{
    public class SetMutePacket : VoiceCraftPacket
    {
        public override PacketType PacketType => PacketType.SetMute;
        
        public int Id { get; private set; }
        public bool Value { get; private set; }

        public SetMutePacket(int id = 0, bool value = false)
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