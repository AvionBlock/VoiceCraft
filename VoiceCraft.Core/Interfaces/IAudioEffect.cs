using System;
using System.Text.Json;
using LiteNetLib.Utils;
using VoiceCraft.Core.Audio.Effects;
using VoiceCraft.Core.World;

namespace VoiceCraft.Core.Interfaces
{
    public interface IAudioEffect : INetSerializable, IDisposable
    {
        EffectType EffectType { get; }

        void Process(VoiceCraftEntity from, VoiceCraftEntity to, ushort effectBitmask, Span<float> data, int count);

        void Reset();

        public static IAudioEffect? FromJsonElement(JsonElement element)
        {
            if (!element.TryGetProperty(nameof(EffectType), out var effectType)) return null;
            if (!effectType.TryGetByte(out var effectTypeByte)) return null;
            var effectTypeValue = (EffectType)effectTypeByte;
            IAudioEffect? audioEffect = null;
            switch (effectTypeValue)
            {
                case EffectType.Visibility:
                    audioEffect = element.Deserialize<VisibilityEffect>();
                    break;
                case EffectType.Proximity:
                    audioEffect = element.Deserialize<ProximityEffect>();
                    break;
                case EffectType.Directional:
                    audioEffect = element.Deserialize<DirectionalEffect>();
                    break;
                case EffectType.ProximityEcho:
                    audioEffect = element.Deserialize<ProximityEchoEffect>();
                    break;
                case EffectType.Echo:
                    audioEffect = element.Deserialize<EchoEffect>();
                    break;
                case EffectType.ProximityMuffle:
                    audioEffect = element.Deserialize<ProximityMuffleEffect>();
                    break;
                case EffectType.Muffle:
                    audioEffect = element.Deserialize<MuffleEffect>();
                    break;
                case EffectType.None:
                default:
                    break;
            }

            return audioEffect;
        }

        public static IAudioEffect? FromReader(EffectType effectType, NetDataReader reader)
        {
            IAudioEffect? audioEffect = null;
            switch (effectType)
            {
                case EffectType.Visibility:
                    audioEffect = new VisibilityEffect();
                    audioEffect.Deserialize(reader);
                    break;
                case EffectType.Proximity:
                    audioEffect = new ProximityEffect();
                    audioEffect.Deserialize(reader);
                    break;
                case EffectType.Directional:
                    audioEffect = new DirectionalEffect();
                    audioEffect.Deserialize(reader);
                    break;
                case EffectType.ProximityEcho:
                    audioEffect = new ProximityEchoEffect();
                    audioEffect.Deserialize(reader);
                    break;
                case EffectType.Echo:
                    audioEffect = new EchoEffect();
                    audioEffect.Deserialize(reader);
                    break;
                case EffectType.ProximityMuffle:
                    audioEffect = new ProximityMuffleEffect();
                    audioEffect.Deserialize(reader);
                    break;
                case EffectType.Muffle:
                    audioEffect = new MuffleEffect();
                    audioEffect.Deserialize(reader);
                    break;
                case EffectType.None:
                default:
                    break;
            }

            return audioEffect;
        }
    }
}