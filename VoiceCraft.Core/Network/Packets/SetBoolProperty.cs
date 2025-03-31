using System.ComponentModel.DataAnnotations;
using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.Packets
{
    public class SetBoolProperty : VoiceCraftPacket
    {
        public override PacketType PacketType { get; }
        public int NetworkId { get; private set; }
        [StringLength(Constants.MaxStringLength)]
        public string Key { get; private set; }
        public bool Value { get; private set; }

        public SetBoolProperty(int networkId = 0, string key = "", bool value = false)
        {
            NetworkId = networkId;
            Key = key;
            Value = value;
        }
        
        public override void Serialize(NetDataWriter writer)
        {
            writer.Put(NetworkId);
            writer.Put(Key);
            writer.Put(Value);
        }

        public override void Deserialize(NetDataReader reader)
        {
            NetworkId = reader.GetInt();
            Key = reader.GetString();
            Value = reader.GetBool();
        }
    }
}