using System.ComponentModel.DataAnnotations;
using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.Packets
{
    public class RemoveFloatProperty : VoiceCraftPacket
    {
        public override PacketType PacketType => PacketType.RemoveFloatProperty;
        public int Id { get; private set; }
        [StringLength(Constants.MaxStringLength)]
        public string Key { get; private set; }

        public RemoveFloatProperty(int id = 0, string key = "")
        {
            Id = id;
            Key = key;
        }
        
        public override void Serialize(NetDataWriter writer)
        {
            writer.Put(Id);
            writer.Put(Key);
        }

        public override void Deserialize(NetDataReader reader)
        {
            Id = reader.GetInt();
            Key = reader.GetString();
        }
    }
}