using System.ComponentModel.DataAnnotations;
using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.Packets
{
    public class SetBoolProperty : VoiceCraftPacket
    {
        public override PacketType PacketType => PacketType.SetBoolProperty;
        public int Id { get; private set; }
        [StringLength(Constants.MaxStringLength)]
        public string Key { get; private set; }
        public bool Value { get; private set; }

        public SetBoolProperty(int id = 0, string key = "", bool value = false)
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
            Value = reader.GetBool();
        }
    }
}