using LiteNetLib.Utils;
using VoiceCraft.Core.Interfaces;

namespace VoiceCraft.Core.Network.Packets
{
    public class AddEffectPacket : VoiceCraftPacket
    {
        public override PacketType PacketType => PacketType.AddEffect;
        
        public int NetworkId { get; set; }
        public EffectType EffectType { get; set; }

        public AddEffectPacket(int networkId, EffectType effectType)
        {
            NetworkId = networkId;
            EffectType = effectType;
        }
        
        public override void Serialize(NetDataWriter writer)
        {
            writer.Put(NetworkId);
            writer.Put((byte)EffectType);
        }

        public override void Deserialize(NetDataReader reader)
        {
            NetworkId = reader.GetInt();
            EffectType = (EffectType)reader.GetByte();
        }
    }
}