using System;
using System.Text.Json.Serialization;
using LiteNetLib.Utils;
using VoiceCraft.Core;
using VoiceCraft.Core.Audio;
using VoiceCraft.Core.World;
using VoiceCraft.Network.Interfaces;

namespace VoiceCraft.Network.Audio.Effects
{
    public class ProximityMuffleEffect : IAudioEffect
    {
        public EffectType EffectType => EffectType.ProximityMuffle;

        [JsonIgnore]
        public ushort Bitmask { get; set; }
        
        public event Action<IAudioEffect>? OnDisposed;

        public float WetDry
        {
            get;
            set => field = Math.Clamp(value, 0.0f, 1.0f);
        } = 1.0f;
        
        public IAudioEffectProcessor GetProcessor(VoiceCraftEntity entity) =>
            new ProximityMuffleEffectProcessor(this, entity);
        
        public void Update(IAudioEffect audioEffect)
        {
            if(audioEffect is not ProximityMuffleEffect proximityMuffleEffect)
                throw new ArgumentException("Unexpected Audio Effect Type!", nameof(audioEffect));
            Bitmask = proximityMuffleEffect.Bitmask;
            WetDry = proximityMuffleEffect.WetDry;
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
    
    public class ProximityMuffleEffectProcessor : IAudioEffectProcessor
    {
        private readonly ProximityMuffleEffect _effect;
        private readonly BiQuadFilter _biQuadFilter;
        
        public IAudioEffect Effect => _effect;
        public VoiceCraftEntity Entity { get; }
        public event Action<IAudioEffectProcessor>? OnDisposed;

        public ProximityMuffleEffectProcessor(ProximityMuffleEffect effect, VoiceCraftEntity entity)
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
            var factor = MathF.Max(Entity.MuffleFactor, to.MuffleFactor);
            
            for (var i = 0; i < buffer.Length; i++)
            {
                var output = _biQuadFilter.Transform(buffer[i]) * factor + buffer[i] * (1.0f - factor);
                buffer[i] = output * _effect.WetDry + buffer[i] * (1.0f - _effect.WetDry);
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
    [JsonSerializable(typeof(ProximityMuffleEffect), GenerationMode = JsonSourceGenerationMode.Metadata)]
    public partial class ProximityMuffleEffectGenerationContext : JsonSerializerContext;
}