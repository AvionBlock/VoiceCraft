using System;
using LiteNetLib.Utils;
using VoiceCraft.Core.Interfaces;

namespace VoiceCraft.Core.Network.Packets
{
    public class SetEffectPacket : VoiceCraftPacket
    {
        public SetEffectPacket(ulong bitmask = 0, IAudioEffect? effect = null)
        {
            Bitmask = bitmask;
            EffectType = effect?.EffectType ?? EffectType.Unknown;
            Effect = effect;
        }

        public override PacketType PacketType => PacketType.SetEffect;

        public ulong Bitmask { get; private set; }
        public EffectType EffectType { get; private set; }
        public IAudioEffect? Effect { get; }

        public override void Serialize(NetDataWriter writer)
        {
            writer.Put(Bitmask);
            writer.Put((byte)(Effect?.EffectType ?? EffectType.Unknown));
            writer.Put(Effect);
        }

        public override void Deserialize(NetDataReader reader)
        {
            Bitmask = reader.GetByte();
            var effectTypeValue = reader.GetByte();
            EffectType = Enum.IsDefined(typeof(EffectType), effectTypeValue)
                ? (EffectType)effectTypeValue
                : EffectType.Unknown;
        }
    }
}