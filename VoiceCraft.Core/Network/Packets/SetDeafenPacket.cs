using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.Packets
{
    public class SetDeafenPacket : VoiceCraftPacket
    {
        public override PacketType PacketType => PacketType.SetDeafen;
        
        public int Id { get; private set; }
        public bool Value { get; private set; }

        public SetDeafenPacket(int id = 0, bool value = true)
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