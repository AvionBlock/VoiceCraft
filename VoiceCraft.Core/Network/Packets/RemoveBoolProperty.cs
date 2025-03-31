using System.ComponentModel.DataAnnotations;
using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.Packets
{
    public class RemoveBoolProperty : VoiceCraftPacket
    {
        public override PacketType PacketType => PacketType.RemoveIntProperty;
        public int NetworkId { get; private set; }
        [StringLength(Constants.MaxStringLength)]
        public string Key { get; private set; }

        public RemoveBoolProperty(int networkId = 0, string key = "")
        {
            NetworkId = networkId;
            Key = key;
        }
        
        public override void Serialize(NetDataWriter writer)
        {
            writer.Put(NetworkId);
            writer.Put(Key);
        }

        public override void Deserialize(NetDataReader reader)
        {
            NetworkId = reader.GetInt();
            Key = reader.GetString();
        }
    }
}