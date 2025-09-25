using System;
using System.Numerics;
using LiteNetLib.Utils;
using VoiceCraft.Core.Interfaces;

namespace VoiceCraft.Core.Audio.Effects
{
    public class ProximityEchoEffect : IAudioEffect
    {
        public EffectType EffectType => EffectType.ProximityEcho;
        private readonly FractionalDelayLine _delayLine;
        private float _delay;
        
        public int SampleRate { get; }
        public float Delay
        {
            get => _delay / SampleRate;
            set
            {
                _delayLine.Ensure(SampleRate, value);
                _delay = SampleRate * value;
            }
        }
        public float Range { get; set; }
        public float Wet { get; set; } = 1f;
        public float Dry { get; set; }
        
        public ProximityEchoEffect(int samplingRate,
            float delay,
            float range = 0f)
        {
            _delayLine = new FractionalDelayLine(samplingRate, delay, InterpolationMode.Nearest);
            SampleRate = samplingRate;
            Delay = delay;
            Range = range;
        }

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

        public void Process(VoiceCraftEntity from, VoiceCraftEntity to, uint effectBitmask, Span<float> data, int count)
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

            for (var i = 0; i < count; i++)
            {
                var delayed = _delayLine.Read(_delay);
                var output = data[i] + delayed * factor;
                _delayLine.Write(output);
                data[i] = output * Wet + data[i] * Dry;
            }
        }

        public void Reset()
        {
            _delayLine.Reset();
        }
        
        public void Dispose()
        {
            //Nothing to dispose.
        }
    }
}