using LiteNetLib.Utils;
using VoiceCraft.Core.Interfaces;

namespace VoiceCraft.Core.Network.Packets
{
    public class SetEffectPacket : VoiceCraftPacket
    {
        public SetEffectPacket(ushort bitmask = 0, IAudioEffect? effect = null)
        {
            Bitmask = bitmask;
            EffectType = effect?.EffectType ?? EffectType.None;
            Effect = effect;
        }

        public override PacketType PacketType => PacketType.SetEffect;

        public ushort Bitmask { get; private set; }
        public EffectType EffectType { get; private set; }
        public IAudioEffect? Effect { get; }

        public override void Serialize(NetDataWriter writer)
        {
            writer.Put(Bitmask);
            writer.Put((byte)(Effect?.EffectType ?? EffectType.None));
            writer.Put(Effect);
        }

        public override void Deserialize(NetDataReader reader)
        {
            Bitmask = reader.GetUShort();
            EffectType = (EffectType)reader.GetByte();
        }
    }
}