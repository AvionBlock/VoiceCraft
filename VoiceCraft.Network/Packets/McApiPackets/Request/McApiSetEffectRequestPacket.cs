using LiteNetLib.Utils;
using VoiceCraft.Network.Interfaces;

namespace VoiceCraft.Network.Packets.McApiPackets.Request;

public class McApiSetEffectRequestPacket : IMcApiPacket
{
    public McApiSetEffectRequestPacket() : this(0, null)
    {
    }

    public McApiSetEffectRequestPacket(ushort bitmask, IAudioEffect? effect)
    {
        Bitmask = bitmask;
        EffectType = effect?.EffectType ?? EffectType.None;
        Effect = effect;
    }

    public ushort Bitmask { get; private set; }
    public EffectType EffectType { get; private set; }
    public IAudioEffect? Effect { get; private set; }

    public McApiPacketType PacketType => McApiPacketType.SetEffectRequest;

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

    public McApiSetEffectRequestPacket Set(ushort bitmask = 0, IAudioEffect? effect = null)
    {
        Bitmask = bitmask;
        EffectType = effect?.EffectType ?? EffectType.None;
        Effect = effect;
        return this;
    }
}