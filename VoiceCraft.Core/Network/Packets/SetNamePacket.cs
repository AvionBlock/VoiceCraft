using System.ComponentModel.DataAnnotations;
using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.Packets
{
    public class SetNamePacket : VoiceCraftPacket
    {
        public override PacketType PacketType => PacketType.SetName;
        [StringLength(Constants.MaxStringLength)]
        public int Id { get; private set; }
        public string Name { get; private set; }
        

        public SetNamePacket(int id = 0, string name = "")
        {
            Id = id;
            Name = name;
        }
        
        public override void Serialize(NetDataWriter writer)
        {
            writer.Put(Id);
            writer.Put(Name);
        }

        public override void Deserialize(NetDataReader reader)
        {
            Id = reader.GetInt();
            Name = reader.GetString();
        }
    }
}