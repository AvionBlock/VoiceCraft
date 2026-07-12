using LiteNetLib.Utils;
using VoiceCraft.Network.Interfaces;

namespace VoiceCraft.Network.Packets.McApiPackets.Event;

public class McApiOnEffectUpdatedPacket(ushort bitmask, IAudioEffect? effect) : IMcApiEventPacket
{
    public McApiOnEffectUpdatedPacket() : this(0, null)
    {
    }

    public EventType EventType => EventType.OnEffectUpdated;
    public ushort Bitmask { get; private set; } = bitmask;
    public EffectType EffectType => Effect?.EffectType ?? EffectType.None;
    public IAudioEffect? Effect { get; private set; } = effect;

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
        Effect?.Bitmask = Bitmask;
    }

    public void Return()
    {
        Effect = null;
        PacketPool<McApiOnEffectUpdatedPacket>.Return(this);
    }

    public void Set(ushort bitmask = 0, IAudioEffect? effect = null)
    {
        Bitmask = bitmask;
        Effect = effect;
    }
}
