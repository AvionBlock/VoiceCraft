using System.ComponentModel.DataAnnotations;
using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.Packets
{
    public class SetNamePacket : VoiceCraftPacket
    {
        public override PacketType PacketType => PacketType.SetName;
        [StringLength(Constants.MaxStringLength)]
        public int NetworkId { get; private set; }
        public string Name { get; private set; }
        

        public SetNamePacket(int networkId = 0, string name = "")
        {
            NetworkId = networkId;
            Name = name;
        }
        
        public override void Serialize(NetDataWriter writer)
        {
            writer.Put(NetworkId);
            writer.Put(Name);
        }

        public override void Deserialize(NetDataReader reader)
        {
            NetworkId = reader.GetInt();
            Name = reader.GetString();
        }
    }
}