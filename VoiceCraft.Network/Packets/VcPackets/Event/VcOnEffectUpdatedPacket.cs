using LiteNetLib.Utils;
using VoiceCraft.Core;
using VoiceCraft.Network.Interfaces;

namespace VoiceCraft.Network.Packets.VcPackets.Event;

public class VcOnEffectUpdatedPacket : IVoiceCraftPacket
{
    public VcOnEffectUpdatedPacket() : this(0, null)
    {
    }

    public VcOnEffectUpdatedPacket(ushort bitmask, IAudioEffect? effect)
    {
        Bitmask = bitmask;
        Effect = effect;
    }

    public ushort Bitmask { get; private set; }
    public EffectType EffectType => Effect?.EffectType ?? EffectType.None;
    public IAudioEffect? Effect { get; private set; }

    public VcPacketType PacketType => VcPacketType.OnEffectUpdated;

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(Bitmask);
        writer.Put((byte)(Effect?.EffectType ?? EffectType.None));
        if (Effect != null)
            writer.Put(Effect);
    }

    public void Deserialize(NetDataReader reader)
    {
        Bitmask = reader.GetUShort();
        var effectType = (EffectType)reader.GetByte();
        Effect = IAudioEffect.FromReader(effectType, reader);
    }

    public VcOnEffectUpdatedPacket Set(ushort bitmask = 0, IAudioEffect? effect = null)
    {
        Bitmask = bitmask;
        Effect = effect;
        return this;
    }
}