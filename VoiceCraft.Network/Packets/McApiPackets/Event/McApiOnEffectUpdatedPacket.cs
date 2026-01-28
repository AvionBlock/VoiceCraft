using LiteNetLib.Utils;
using VoiceCraft.Network.Interfaces;

namespace VoiceCraft.Network.Packets.McApiPackets.Event;

public class McApiOnEffectUpdatedPacket(ushort bitmask, IAudioEffect? effect) : IMcApiPacket
{
    public McApiOnEffectUpdatedPacket() : this(0, null)
    {
    }

    public ushort Bitmask { get; private set; } = bitmask;
    public EffectType EffectType { get; private set; } = effect?.EffectType ?? EffectType.None;
    public IAudioEffect? Effect { get; private set; } = effect;

    public McApiPacketType PacketType => McApiPacketType.OnEffectUpdated;

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
        EffectType = (EffectType)reader.GetByte();
    }

    public McApiOnEffectUpdatedPacket Set(ushort bitmask = 0, IAudioEffect? effect = null)
    {
        Bitmask = bitmask;
        EffectType = effect?.EffectType ?? EffectType.None;
        Effect = effect;
        return this;
    }
}