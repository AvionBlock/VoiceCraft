using System;
using System.Numerics;
using System.Text.Json.Serialization;
using LiteNetLib.Utils;
using VoiceCraft.Core;
using VoiceCraft.Core.Audio;
using VoiceCraft.Core.Interfaces;
using VoiceCraft.Core.World;
using VoiceCraft.Network.Interfaces;

namespace VoiceCraft.Network.Audio.Effects
{
    public class ProximityEffect : IAudioEffect, IVisible
    {
        public EffectType EffectType => EffectType.Proximity;

        [JsonIgnore] public ushort Bitmask { get; set; }

        public event Action<IAudioEffect>? OnDisposed;

        public float MinRange { get; set; }

        public float MaxRange { get; set; }

        public float WetDry
        {
            get;
            set => field = Math.Clamp(value, 0.0f, 1.0f);
        } = 1.0f;

        public IAudioEffectProcessor GetProcessor(VoiceCraftEntity entity) =>
            new ProximityEffectProcessor(this, entity);

        public void Update(IAudioEffect audioEffect)
        {
            if (audioEffect is not ProximityEffect proximityEffect)
                throw new ArgumentException("Unexpected Audio Effect Type!", nameof(audioEffect));
            Bitmask = proximityEffect.Bitmask;
            MinRange = proximityEffect.MinRange;
            MaxRange = proximityEffect.MaxRange;
            WetDry = proximityEffect.WetDry;
        }
        
        public float EvaluateMinRangeProperty(VoiceCraftEntity e1, VoiceCraftEntity e2)
        {
            const string property = $"{nameof(ProximityEffect)}:{nameof(MinRange)}";
            var propVal1 = e1.TryGetProperty<float?>(property, out var prop1);
            var propVal2 = e2.TryGetProperty<float?>(property, out var prop2);
            if (!propVal1 && !propVal2) return MinRange;
            return Math.Min(prop1 ?? float.MaxValue, prop2 ?? float.MaxValue);
        }
        
        public float EvaluateMaxRangeProperty(VoiceCraftEntity e1, VoiceCraftEntity e2)
        {
            const string property = $"{nameof(ProximityEffect)}:{nameof(MaxRange)}";
            var propVal1 = e1.TryGetProperty<float?>(property, out var prop1);
            var propVal2 = e2.TryGetProperty<float?>(property, out var prop2);
            if (!propVal1 && !propVal2) return MaxRange;
            return Math.Max(prop1 ?? float.MinValue, prop2 ?? float.MinValue);
        }
        
        public float EvaluateWetDryProperty(VoiceCraftEntity e1, VoiceCraftEntity e2)
        {
            const string property = $"{nameof(ProximityEffect)}:{nameof(WetDry)}";
            var propVal1 = e1.TryGetProperty<float?>(property, out var prop1);
            var propVal2 = e2.TryGetProperty<float?>(property, out var prop2);
            if (!propVal1 && !propVal2) return WetDry;
            return Math.Clamp(Math.Max(prop1 ?? 0.0f, prop2 ?? 0.0f), 0.0f, 1.0f);
        }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(MinRange);
            writer.Put(MaxRange);
            writer.Put(WetDry);
        }

        public void Deserialize(NetDataReader reader)
        {
            MinRange = reader.GetFloat();
            MaxRange = reader.GetFloat();
            WetDry = reader.GetFloat();
        }

        public bool Visibility(VoiceCraftEntity from, VoiceCraftEntity to, ushort effectBitmask)
        {
            var bitmask = from.TalkBitmask & to.ListenBitmask & from.EffectBitmask & to.EffectBitmask;
            if ((bitmask & effectBitmask) == 0) return true; //Proximity checking disabled.
            var maxRange = EvaluateMaxRangeProperty(from, to);
            var distance = Vector3.Distance(from.Position, to.Position);
            return distance <= maxRange;
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

    public class ProximityEffectProcessor : IAudioEffectProcessor
    {
        private readonly ProximityEffect _effect;
        private readonly SampleLerpVolume _lerpVolume;
        private bool _disposed;

        public IAudioEffect Effect => _effect;
        public VoiceCraftEntity Entity { get; }
        public event Action<IAudioEffectProcessor>? OnDisposed;

        public ProximityEffectProcessor(ProximityEffect effect, VoiceCraftEntity entity)
        {
            _effect = effect;
            Entity = entity;
            _lerpVolume = new SampleLerpVolume(Constants.SampleRate, TimeSpan.FromMilliseconds(20));
            Effect.OnDisposed += OnEffectDisposed;
        }

        public void Process(VoiceCraftEntity to, Span<float> buffer)
        {
            var bitmask = Entity.TalkBitmask & to.ListenBitmask & Entity.EffectBitmask & to.EffectBitmask;
            if ((bitmask & Effect.Bitmask) == 0) return;

            //Cache Values
            var wet = _effect.EvaluateWetDryProperty(Entity, to);
            var dry = 1.0f - wet;
            var minRange = _effect.EvaluateMinRangeProperty(Entity, to);
            var maxRange = _effect.EvaluateMaxRangeProperty(Entity, to);
            var range = maxRange - minRange;
            if (range == 0) return; //Range is 0. Do not calculate division.
            
            var distance = Vector3.Distance(Entity.Position, to.Position) - minRange;
            var factor = 1f - Math.Clamp(distance / range, 0f, 1f);
            _lerpVolume.TargetVolume = factor;

            for (var i = 0; i < buffer.Length; i += Constants.PlaybackChannels)
            {
                for (var channel = 0; channel < Constants.PlaybackChannels && i + channel < buffer.Length; channel++)
                {
                    var index = i + channel;
                    var output = _lerpVolume.Transform(buffer[index]);
                    buffer[index] = output * wet + buffer[index] * dry;
                }

                _lerpVolume.Step();
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            Effect.OnDisposed -= OnEffectDisposed;
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

        private void OnEffectDisposed(IAudioEffect _)
        {
            Dispose();
        }
    }

    [JsonSourceGenerationOptions(WriteIndented = true)]
    [JsonSerializable(typeof(ProximityEffect), GenerationMode = JsonSourceGenerationMode.Metadata)]
    public partial class ProximityEffectGenerationContext : JsonSerializerContext;
}
