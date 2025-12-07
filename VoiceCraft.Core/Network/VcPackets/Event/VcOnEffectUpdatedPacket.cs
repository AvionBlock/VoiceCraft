using LiteNetLib.Utils;
using VoiceCraft.Core.Interfaces;

namespace VoiceCraft.Core.Network.VcPackets.Event
{
    public class VcOnEffectUpdatedPacket : IVoiceCraftPacket
    {
        public VcOnEffectUpdatedPacket() : this(0, null)
        {
        }

        public VcOnEffectUpdatedPacket(ushort bitmask, IAudioEffect? effect)
        {
            Bitmask = bitmask;
            EffectType = effect?.EffectType ?? EffectType.None;
            Effect = effect;
        }

        public VcPacketType PacketType => VcPacketType.OnEffectUpdated;

        public ushort Bitmask { get; private set; }
        public EffectType EffectType { get; private set; }
        public IAudioEffect? Effect { get; private set; }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(Bitmask);
            writer.Put((byte)(Effect?.EffectType ?? EffectType.None));
            writer.Put(Effect);
        }

        public void Deserialize(NetDataReader reader)
        {
            Bitmask = reader.GetUShort();
            EffectType = (EffectType)reader.GetByte();
        }

        public VcOnEffectUpdatedPacket Set(ushort bitmask = 0, IAudioEffect? effect = null)
        {
            Bitmask = bitmask;
            EffectType = effect?.EffectType ?? EffectType.None;
            Effect = effect;
            return this;
        }
    }
}