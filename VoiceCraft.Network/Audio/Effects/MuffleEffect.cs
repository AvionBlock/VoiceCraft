using System;
using System.Text.Json.Serialization;
using LiteNetLib.Utils;
using VoiceCraft.Core;
using VoiceCraft.Core.Audio;
using VoiceCraft.Core.World;
using VoiceCraft.Network.Interfaces;

namespace VoiceCraft.Network.Audio.Effects
{
    public class MuffleEffect : IAudioEffect
    {
        public EffectType EffectType => EffectType.Muffle;

        [JsonIgnore] public ushort Bitmask { get; set; }

        public event Action<IAudioEffect>? OnDisposed;

        public float WetDry
        {
            get;
            set => field = Math.Clamp(value, 0.0f, 1.0f);
        } = 1.0f;

        public IAudioEffectProcessor GetProcessor(VoiceCraftEntity entity) =>
            new MuffleEffectProcessor(this, entity);

        public void Update(IAudioEffect audioEffect)
        {
            if (audioEffect is not MuffleEffect muffleEffect)
                throw new ArgumentException("Unexpected Audio Effect Type!", nameof(audioEffect));
            Bitmask = muffleEffect.Bitmask;
            WetDry = muffleEffect.WetDry;
        }
        
        public float EvaluateWetDryProperty(VoiceCraftEntity e1, VoiceCraftEntity e2)
        {
            const string property = $"{nameof(MuffleEffect)}:{nameof(WetDry)}";
            var propVal1 = e1.TryGetProperty<float?>(property, out var prop1);
            var propVal2 = e2.TryGetProperty<float?>(property, out var prop2);
            if (!propVal1 && !propVal2) return WetDry;
            return Math.Clamp(Math.Max(prop1 ?? 0.0f, prop2 ?? 0.0f), 0.0f, 1.0f);
        }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(WetDry);
        }

        public void Deserialize(NetDataReader reader)
        {
            WetDry = reader.GetFloat();
        }

        public void Dispose()
        {
            try
            {
                OnDisposed?.Invoke(this);
            }
            finally
            {
                OnDisposed = null;
                GC.SuppressFinalize(this);
            }
        }
    }

    public class MuffleEffectProcessor : IAudioEffectProcessor
    {
        private readonly MuffleEffect _effect;
        private readonly BiQuadFilter _biQuadFilter;

        public IAudioEffect Effect => _effect;
        public VoiceCraftEntity Entity { get; }
        public event Action<IAudioEffectProcessor>? OnDisposed;

        public MuffleEffectProcessor(MuffleEffect effect, VoiceCraftEntity entity)
        {
            _effect = effect;
            Entity = entity;
            _biQuadFilter = new BiQuadFilter();
            Effect.OnDisposed += _ => Dispose();
            _biQuadFilter.SetLowPassFilter(Constants.SampleRate, 200, 1);
        }

        public void Process(VoiceCraftEntity to, Span<float> buffer)
        {
            var bitmask = Entity.TalkBitmask & to.ListenBitmask & Entity.EffectBitmask & to.EffectBitmask;
            if ((bitmask & Effect.Bitmask) == 0) return;

            //Cache Values
            var wet = _effect.EvaluateWetDryProperty(Entity, to);
            var dry = 1.0f - wet;

            for (var i = 0; i < buffer.Length; i++)
            {
                var output = _biQuadFilter.Transform(buffer[i]);
                buffer[i] = output * wet + buffer[i] * dry;
            }
        }

        public void Dispose()
        {
            try
            {
                OnDisposed?.Invoke(this);
            }
            finally
            {
                GC.SuppressFinalize(this);
            }
        }
    }

    [JsonSourceGenerationOptions(WriteIndented = true)]
    [JsonSerializable(typeof(MuffleEffect), GenerationMode = JsonSourceGenerationMode.Metadata)]
    public partial class MuffleEffectGenerationContext : JsonSerializerContext;
}