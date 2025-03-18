using LiteNetLib.Utils;
using VoiceCraft.Core.Interfaces;

namespace VoiceCraft.Core.Network.Packets
{
    public class SetEffectPacket : VoiceCraftPacket
    {
        public override PacketType PacketType => PacketType.SetEffect;
        
        public int NetworkId { get; set; }
        public EffectType EffectType { get; set; }
        public IAudioEffect Effect { get; set; }

        public SetEffectPacket(int networkId, IAudioEffect effect)
        {
            NetworkId = networkId;
            EffectType = effect.EffectType;
            Effect = effect;
        }
        
        public override void Serialize(NetDataWriter writer)
        {
            writer.Put(NetworkId);
            writer.Put((byte)EffectType);
            writer.Put(Effect);
        }

        public override void Deserialize(NetDataReader reader)
        {
            throw new System.NotImplementedException();
        }
    }
}