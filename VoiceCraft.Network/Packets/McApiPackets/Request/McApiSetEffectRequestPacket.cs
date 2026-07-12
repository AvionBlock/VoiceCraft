using LiteNetLib.Utils;
using VoiceCraft.Network.Interfaces;

namespace VoiceCraft.Network.Packets.McApiPackets.Request;

public class McApiSetEffectRequestPacket(ushort bitmask, IAudioEffect? effect) : IMcApiPacket
{
    public McApiSetEffectRequestPacket() : this(0, null)
    {
    }

    public ushort Bitmask { get; private set; } = bitmask;
    public EffectType EffectType => Effect?.EffectType ?? EffectType.None;
    public IAudioEffect? Effect { get; private set; } = effect;

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
        var effectType = (EffectType)reader.GetByte();
        Effect = IAudioEffect.FromReader(effectType, reader);
        Effect?.Bitmask = Bitmask;
    }
    
    public void Return()
    {
        PacketPool<McApiSetEffectRequestPacket>.Return(this);
    }

    public void Set(ushort bitmask = 0, IAudioEffect? effect = null)
    {
        Bitmask = bitmask;
        Effect = effect;
    }
}