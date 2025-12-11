using LiteNetLib.Utils;
using VoiceCraft.Core.Interfaces;

namespace VoiceCraft.Core.Network.McApiPackets.Request
{
    public class McApiSetEffectRequestPacket : IMcApiPacket
    {
        public McApiSetEffectRequestPacket() : this(string.Empty, 0, null)
        {
        }

        public McApiSetEffectRequestPacket(string token, ushort bitmask, IAudioEffect? effect)
        {
            Token = token;
            Bitmask = bitmask;
            EffectType = effect?.EffectType ?? EffectType.None;
            Effect = effect;
        }

        public McApiPacketType PacketType => McApiPacketType.SetEffectRequest;

        public string Token { get; private set; }
        public ushort Bitmask { get; private set; }
        public EffectType EffectType { get; private set; }
        public IAudioEffect? Effect { get; private set; }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(Token, Constants.MaxStringLength);
            writer.Put(Bitmask);
            writer.Put((byte)(Effect?.EffectType ?? EffectType.None));
            writer.Put(Effect);
        }

        public void Deserialize(NetDataReader reader)
        {
            Token = reader.GetString(Constants.MaxStringLength);
            Bitmask = reader.GetUShort();
            EffectType = (EffectType)reader.GetByte();
        }

        public McApiSetEffectRequestPacket Set(string token = "", ushort bitmask = 0, IAudioEffect? effect = null)
        {
            Token = token;
            Bitmask = bitmask;
            EffectType = effect?.EffectType ?? EffectType.None;
            Effect = effect;
            return this;
        }
    }
}