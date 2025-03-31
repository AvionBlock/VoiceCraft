using System.ComponentModel.DataAnnotations;
using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.Packets
{
    public class SetFloatProperty : VoiceCraftPacket
    {
        public override PacketType PacketType => PacketType.SetFloatProperty;
        public int Id { get; private set; }
        [StringLength(Constants.MaxStringLength)]
        public string Key { get; private set; }
        public float Value { get; private set; }

        public SetFloatProperty(int id = 0, string key = "", float value = 0f)
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
            Value = reader.GetFloat();
        }
    }
}