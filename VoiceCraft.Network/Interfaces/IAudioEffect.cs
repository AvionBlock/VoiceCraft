using System;
using System.Text.Json;
using LiteNetLib.Utils;
using VoiceCraft.Core.World;
using VoiceCraft.Network.Audio.Effects;

namespace VoiceCraft.Network.Interfaces
{
    public interface IAudioEffect : INetSerializable, IDisposable
    {
        EffectType EffectType { get; }
        ushort Bitmask { get; set; }
        event Action<IAudioEffect>? OnDisposed;
        void Update(IAudioEffect audioEffect);
        IAudioEffectProcessor GetProcessor(VoiceCraftEntity entity);

        public static IAudioEffect? FromJsonElement(JsonElement element)
        {
            if (!element.TryGetProperty(nameof(EffectType), out var effectType)) return null;
            if (!effectType.TryGetByte(out var effectTypeByte)) return null;
            var effectTypeValue = (EffectType)effectTypeByte;
            IAudioEffect? audioEffect = null;
            switch (effectTypeValue)
            {
                case EffectType.Visibility:
                    audioEffect =
                        element.Deserialize<VisibilityEffect>(
                            VisibilityEffectGenerationContext.Default.VisibilityEffect);
                    break;
                case EffectType.Proximity:
                    audioEffect =
                        element.Deserialize<ProximityEffect>(
                            ProximityEffectGenerationContext.Default.ProximityEffect);
                    break;
                case EffectType.Directional:
                    audioEffect =
                        element.Deserialize<DirectionalEffect>(
                            DirectionalEffectGenerationContext.Default.DirectionalEffect);
                    break;
                case EffectType.ProximityEcho:
                    audioEffect =
                        element.Deserialize<ProximityEchoEffect>(
                            ProximityEchoEffectGenerationContext.Default.ProximityEchoEffect);
                    break;
                case EffectType.Echo:
                    audioEffect = element.Deserialize<EchoEffect>(
                        EchoEffectGenerationContext.Default.EchoEffect);
                    break;
                case EffectType.ProximityMuffle:
                    audioEffect =
                        element.Deserialize<ProximityMuffleEffect>(
                            ProximityMuffleEffectGenerationContext.Default.ProximityMuffleEffect);
                    break;
                case EffectType.Muffle:
                    audioEffect = element.Deserialize<MuffleEffect>(
                        MuffleEffectGenerationContext.Default.MuffleEffect);
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
                    break;
                case EffectType.Proximity:
                    audioEffect = new ProximityEffect();
                    break;
                case EffectType.Directional:
                    audioEffect = new DirectionalEffect();
                    break;
                case EffectType.ProximityEcho:
                    audioEffect = new ProximityEchoEffect();
                    break;
                case EffectType.Echo:
                    audioEffect = new EchoEffect();
                    break;
                case EffectType.ProximityMuffle:
                    audioEffect = new ProximityMuffleEffect();
                    break;
                case EffectType.Muffle:
                    audioEffect = new MuffleEffect();
                    break;
                case EffectType.None:
                default:
                    break;
            }
            
            audioEffect?.Deserialize(reader);
            return audioEffect;
        }
    }
}