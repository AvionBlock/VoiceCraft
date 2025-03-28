using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.Packets
{
    public class RemoveEffectPacket : VoiceCraftPacket
    {
        public override PacketType PacketType => PacketType.SetEffect;
        
        public int NetworkId { get; set; }
        public EffectType EffectType { get; set; }

        public RemoveEffectPacket(int networkId, EffectType effectType)
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