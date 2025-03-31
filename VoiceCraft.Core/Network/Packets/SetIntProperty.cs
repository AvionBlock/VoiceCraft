using System.ComponentModel.DataAnnotations;
using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.Packets
{
    public class SetIntProperty : VoiceCraftPacket
    {
        public override PacketType PacketType => PacketType.SetIntProperty;
        public int Id { get; private set; }
        [StringLength(Constants.MaxStringLength)]
        public string Key { get; private set; }
        public int Value { get; private set; }

        public SetIntProperty(int id = 0, string key = "", int value = 0)
        {
            Id = id;
            Key = key;
            Value = value;
        }
        
        public override void Serialize(NetDataWriter writer)
        {
            writer.Put(Id);
            writer.Put(Key);
            writer.Put(Value);
        }

        public override void Deserialize(NetDataReader reader)
        {
            Id = reader.GetInt();
            Key = reader.GetString();
            Value = reader.GetInt();
        }
    }
}