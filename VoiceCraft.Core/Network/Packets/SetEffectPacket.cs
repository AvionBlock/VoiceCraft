using LiteNetLib.Utils;
using VoiceCraft.Core.Interfaces;

namespace VoiceCraft.Core.Network.Packets
{
    public class SetEffectPacket : VoiceCraftPacket
    {
        public override PacketType PacketType => PacketType.SetEffect;
        
        public int NetworkId { get; set; }
        public EffectType EffectType { get; set; }
        public IAudioEffect? Effect { get; set; }

        public SetEffectPacket(int networkId, IAudioEffect? effect)
        {
            NetworkId = networkId;
            EffectType = effect?.EffectType ?? EffectType.Unknown;
            Effect = effect;
        }
        
        public override void Serialize(NetDataWriter writer)
        {
            writer.Put(NetworkId);
            writer.Put((byte)(Effect?.EffectType ?? EffectType.Unknown));
            writer.Put(Effect);
        }

        public override void Deserialize(NetDataReader reader)
        {
            NetworkId = reader.GetInt();
            EffectType = (EffectType)reader.GetByte();
        }
    }
}