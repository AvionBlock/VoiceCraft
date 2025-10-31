using System;
using System.Collections.Generic;
using System.Numerics;
using LiteNetLib.Utils;
using VoiceCraft.Core.Interfaces;
using VoiceCraft.Core.World;

namespace VoiceCraft.Core.Audio.Effects
{
    public class ProximityEchoEffect : IAudioEffect
    {
        private readonly Dictionary<VoiceCraftEntity, FractionalDelayLine> _delayLines =
            new Dictionary<VoiceCraftEntity, FractionalDelayLine>();
        private float _delay;

        public ProximityEchoEffect(int samplingRate,
            float delay,
            float range = 0f)
        {
            SampleRate = samplingRate;
            Delay = delay;
            Range = range;
        }

        public int SampleRate { get; }
        public float Delay
        {
            get => _delay / SampleRate;
            set => _delay = SampleRate * value;
        }
        public float Range { get; set; }
        public float Wet { get; set; } = 1f;
        public float Dry { get; set; }
        public EffectType EffectType => EffectType.ProximityEcho;

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(Delay);
            writer.Put(Range);
            writer.Put(Wet);
            writer.Put(Dry);
        }

        public void Deserialize(NetDataReader reader)
        {
            Delay = reader.GetFloat();
            Range = reader.GetFloat();
            Wet = reader.GetFloat();
            Dry = reader.GetFloat();
        }

        public void Process(VoiceCraftEntity from, VoiceCraftEntity to, ushort effectBitmask, Span<float> data, int count)
        {
            var bitmask = from.TalkBitmask & to.ListenBitmask & from.EffectBitmask & to.EffectBitmask;
            if ((bitmask & effectBitmask) == 0)
                return; //There may still be echo from the entity itself but that will phase out over time.
            
            var factor = 0f;
            if (Range != 0)
            {
                var range = Math.Clamp(Vector3.Distance(from.Position, to.Position) / Range, 0.0f,
                    1.0f); //The range at which the echo will take effect.
                factor = Math.Max(from.CaveFactor, to.CaveFactor) * range;
            }
            
            var delayLine = GetOrCreateDelayLine(from);
            delayLine.Ensure(SampleRate, Delay);

            for (var i = 0; i < count; i++)
            {
                var delayed = delayLine.Read(_delay);
                var output = data[i] + delayed * factor;
                delayLine.Write(output);
                data[i] = output * Wet + data[i] * Dry;
            }
        }

        public void Reset()
        {
            lock (_delayLines)
            {
                _delayLines.Clear();
            }
        }

        public void Dispose()
        {
            //Nothing to dispose.
        }

        private FractionalDelayLine GetOrCreateDelayLine(VoiceCraftEntity entity)
        {
            lock (_delayLines)
            {
                if (_delayLines.TryGetValue(entity, out var delayLine))
                    return delayLine;
                delayLine = new FractionalDelayLine(SampleRate, Delay, InterpolationMode.Nearest);
                _delayLines.TryAdd(entity, delayLine);
                entity.OnDestroyed += RemoveDelayLine;
                return delayLine;
            }
        }

        private void RemoveDelayLine(VoiceCraftEntity entity)
        {
            lock (_delayLines)
            {
                _delayLines.Remove(entity);
                entity.OnDestroyed -= RemoveDelayLine;
            }
        }
    }
}