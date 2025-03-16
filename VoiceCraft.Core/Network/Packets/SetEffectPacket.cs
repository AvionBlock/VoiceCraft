using LiteNetLib.Utils;
using VoiceCraft.Core.Interfaces;

namespace VoiceCraft.Core.Network.Packets
{
    public class SetEffectPacket : VoiceCraftPacket
    {
        public override PacketType PacketType => PacketType.SetEffect;
        public int NetworkId { get; set; }
        public EffectType EffectType { get; private set; } = EffectType.Unknown;
        public IAudioEffect? Effect { get; set; }
        
        public override void Serialize(NetDataWriter writer)
        {
            writer.Put(NetworkId);
            writer.Put((byte?)Effect?.EffectType ?? (byte)EffectType.Unknown);
        }

        public override void Deserialize(NetDataReader reader)
        {
            NetworkId = reader.GetInt();
            EffectType = (EffectType)reader.GetByte();
        }
    }
}