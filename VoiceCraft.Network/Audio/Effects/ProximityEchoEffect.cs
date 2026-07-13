using System;
using System.Numerics;
using System.Text.Json.Serialization;
using LiteNetLib.Utils;
using VoiceCraft.Core;
using VoiceCraft.Core.Audio;
using VoiceCraft.Core.World;
using VoiceCraft.Network.Interfaces;

namespace VoiceCraft.Network.Audio.Effects
{
    public class ProximityEchoEffect : IAudioEffect
    {
        public static int SampleRate => Constants.SampleRate;

        public EffectType EffectType => EffectType.ProximityEcho;

        [JsonIgnore] public ushort Bitmask { get; set; }

        public event Action<IAudioEffect>? OnDisposed;

        public float Delay
        {
            get => field / SampleRate;
            set => field = SampleRate * Math.Clamp(value, 0.0f, 10.0f);
        }

        public float Range
        {
            get;
            set => field = Math.Max(value, 0.0f);
        }

        public float Factor
        {
            get;
            set => field = Math.Clamp(value, 0.0f, 1.0f);
        } = 0.0f;

        public float WetDry
        {
            get;
            set => field = Math.Clamp(value, 0.0f, 1.0f);
        } = 1.0f;

        public ProximityEchoEffect()
        {
            Delay = 0.5f;
        }

        public IAudioEffectProcessor GetProcessor(VoiceCraftEntity entity) =>
            new ProximityEchoEffectProcessor(this, entity);

        public void Update(IAudioEffect audioEffect)
        {
            if (audioEffect is not ProximityEchoEffect proximityEchoEffect)
                throw new ArgumentException("Unexpected Audio Effect Type!", nameof(audioEffect));
            Bitmask = proximityEchoEffect.Bitmask;
            Delay = proximityEchoEffect.Delay;
            Range = proximityEchoEffect.Range;
            Factor = proximityEchoEffect.Factor;
            WetDry = proximityEchoEffect.WetDry;
        }

        public float EvaluateDelayProperty(VoiceCraftEntity e1, VoiceCraftEntity e2)
        {
            const string property = $"{nameof(ProximityEchoEffect)}:{nameof(Delay)}";
            var propVal1 = e1.TryGetProperty<float?>(property, out var prop1);
            var propVal2 = e2.TryGetProperty<float?>(property, out var prop2);
            if (!propVal1 && !propVal2) return Delay;
            return Math.Clamp(Math.Max(prop1 ?? 0.0f, prop2 ?? 0.0f), 0.0f, 10.0f);
        }

        public float EvaluateRangeProperty(VoiceCraftEntity e1, VoiceCraftEntity e2)
        {
            const string property = $"{nameof(ProximityEchoEffect)}:{nameof(Range)}";
            var propVal1 = e1.TryGetProperty<float?>(property, out var prop1);
            var propVal2 = e2.TryGetProperty<float?>(property, out var prop2);
            if (!propVal1 && !propVal2) return Range;
            return Math.Max(Math.Max(prop1 ?? 0.0f, prop2 ?? 0.0f), 0.0f); //Only Positive Integers.
        }

        public float EvaluateFactorProperty(VoiceCraftEntity e1, VoiceCraftEntity e2)
        {
            const string property = $"{nameof(ProximityEchoEffect)}:{nameof(Factor)}";
            var propVal1 = e1.TryGetProperty<float?>(property, out var prop1);
            var propVal2 = e2.TryGetProperty<float?>(property, out var prop2);
            if (!propVal1 && !propVal2) return Factor;
            return Math.Clamp(Math.Max(prop1 ?? 0.0f, prop2 ?? 0.0f), 0.0f, 1.0f);
        }

        public float EvaluateWetDryProperty(VoiceCraftEntity e1, VoiceCraftEntity e2)
        {
            const string property = $"{nameof(ProximityEchoEffect)}:{nameof(WetDry)}";
            var propVal1 = e1.TryGetProperty<float?>(property, out var prop1);
            var propVal2 = e2.TryGetProperty<float?>(property, out var prop2);
            if (!propVal1 && !propVal2) return WetDry;
            return Math.Clamp(Math.Max(prop1 ?? 0.0f, prop2 ?? 0.0f), 0.0f, 1.0f);
        }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(Delay);
            writer.Put(Range);
            writer.Put(Factor);
            writer.Put(WetDry);
        }

        public void Deserialize(NetDataReader reader)
        {
            Delay = reader.GetFloat();
            Range = reader.GetFloat();
            Factor = reader.GetFloat();
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

    public class ProximityEchoEffectProcessor : IAudioEffectProcessor
    {
        private readonly ProximityEchoEffect _effect;
        private readonly FractionalDelayLine[] _delayLines;
        private bool _disposed;

        public IAudioEffect Effect => _effect;
        public VoiceCraftEntity Entity { get; }
        public event Action<IAudioEffectProcessor>? OnDisposed;

        public ProximityEchoEffectProcessor(ProximityEchoEffect effect, VoiceCraftEntity entity)
        {
            _effect = effect;
            Entity = entity;
            _delayLines = new FractionalDelayLine[Constants.PlaybackChannels];
            for (var channel = 0; channel < _delayLines.Length; channel++)
                _delayLines[channel] =
                    new FractionalDelayLine(Constants.SampleRate, _effect.Delay, InterpolationMode.Nearest);
            Effect.OnDisposed += OnEffectDisposed;
        }

        public void Process(VoiceCraftEntity to, Span<float> buffer)
        {
            var bitmask = Entity.TalkBitmask & to.ListenBitmask & Entity.EffectBitmask & to.EffectBitmask;
            if ((bitmask & Effect.Bitmask) == 0) return;

            var factor = 0.0f;
            var range = _effect.EvaluateRangeProperty(Entity, to);
            if (range != 0) //Range is 0. Do not calculate division.
            {
                //The range at which the echo will take effect. Never set to 1.0 as it may cause infinite echo.
                var distance = Vector3.Distance(Entity.Position, to.Position);
                range = Math.Clamp(distance / range, 0.0f, 0.9f);
                factor = _effect.EvaluateFactorProperty(Entity, to) * range;
            }

            //Cache Values
            var wet = _effect.EvaluateWetDryProperty(Entity, to);
            var dry = 1.0f - wet;
            var delay = _effect.EvaluateDelayProperty(Entity, to);
            foreach (var delayLine in _delayLines)
                delayLine.Ensure(ProximityEchoEffect.SampleRate, delay);
            var delaySamples = delay * ProximityEchoEffect.SampleRate;

            for (var i = 0; i < buffer.Length; i += Constants.PlaybackChannels)
            {
                for (var channel = 0; channel < Constants.PlaybackChannels && i + channel < buffer.Length; channel++)
                {
                    var index = i + channel;
                    var delayed = _delayLines[channel].Read(delaySamples) * factor;
                    var output = buffer[index] + delayed;
                    _delayLines[channel].Write(output);
                    buffer[index] = output * wet + buffer[index] * dry;
                }
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
    [JsonSerializable(typeof(ProximityEchoEffect), GenerationMode = JsonSourceGenerationMode.Metadata)]
    public partial class ProximityEchoEffectGenerationContext : JsonSerializerContext;
}
