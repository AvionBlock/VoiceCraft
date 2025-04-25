using System;
using LiteNetLib.Utils;
using VoiceCraft.Core.Interfaces;

namespace VoiceCraft.Core.Network.Packets
{
    public class SetEffectPacket : VoiceCraftPacket
    {
        public override PacketType PacketType => PacketType.SetEffect;
        
        public byte Index { get; private set; }
        public EffectType EffectType { get; private set; }
        public IAudioEffect? Effect { get; private set; }

        public SetEffectPacket(byte index = 0, IAudioEffect? effect = null)
        {
            Index = index;
            EffectType = effect?.EffectType ?? EffectType.Unknown;
            Effect = effect;
        }
        
        public override void Serialize(NetDataWriter writer)
        {
            writer.Put(Index);
            writer.Put((byte)(Effect?.EffectType ?? EffectType.Unknown));
            writer.Put(Effect);
        }

        public override void Deserialize(NetDataReader reader)
        {
            Index = reader.GetByte();
            var effectTypeValue = reader.GetByte();
            EffectType = Enum.IsDefined(typeof(EffectType), effectTypeValue) ? (EffectType)effectTypeValue : EffectType.Unknown;
        }
    }
}