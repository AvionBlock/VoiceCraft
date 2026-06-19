using LiteNetLib.Utils;
using VoiceCraft.Network.Interfaces;

namespace VoiceCraft.Network.Packets.VcPackets.Event;

public class VcOnEffectUpdatedPacket(ushort bitmask, IAudioEffect? effect) : IVoiceCraftEventPacket
{
    public VcOnEffectUpdatedPacket() : this(0, null)
    {
    }

    public EventType EventType => EventType.OnEffectUpdated;
    public ushort Bitmask { get; private set; } = bitmask;
    public EffectType EffectType => Effect?.EffectType ?? EffectType.None;
    public IAudioEffect? Effect { get; private set; } = effect;

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(Bitmask);
        writer.Put((byte)EffectType);
        if (Effect != null)
            writer.Put(Effect);
    }

    public void Deserialize(NetDataReader reader)
    {
        Bitmask = reader.GetUShort();
        var effectType = (EffectType)reader.GetByte();
        Effect = IAudioEffect.FromReader(effectType, reader);
        Effect?.Bitmask = Bitmask;
    }

    public VcOnEffectUpdatedPacket Set(ushort bitmask = 0, IAudioEffect? effect = null)
    {
        Bitmask = bitmask;
        Effect = effect;
        return this;
    }
}