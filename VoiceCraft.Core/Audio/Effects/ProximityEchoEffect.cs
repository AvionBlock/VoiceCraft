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
        public float Feedback { get; set; }
        public float Range { get; set; }
        
        public ProximityEchoEffect(int samplingRate,
            float delay,
            float feedback = 0.5f,
            float range = 0f)
        {
            _delayLine = new FractionalDelayLine(samplingRate, delay, InterpolationMode.Nearest);
            SampleRate = samplingRate;
            Delay = delay;
            Feedback = feedback;
            Range = range;
        }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(Delay);
            writer.Put(Feedback);
            writer.Put(Range);
        }

        public void Deserialize(NetDataReader reader)
        {
            Delay = reader.GetFloat();
            Feedback = reader.GetFloat();
            Range = reader.GetFloat();
        }

        public void Process(VoiceCraftEntity from, VoiceCraftEntity to, uint effectBitmask, Span<float> data, int count)
        {
            var bitmask = from.TalkBitmask & to.ListenBitmask & from.EffectBitmask & to.EffectBitmask;
            if ((bitmask & effectBitmask) == 0)
                return; //There may still be echo from the entity itself but that will phase out over time.

            var range = 1f;
            if(Range != 0)
                range = Math.Clamp(Vector3.Distance(from.Position, to.Position) / Range, 0.0f, 1.0f);
            
            var factor = Math.Max(from.CaveFactor, to.CaveFactor) * range;
            for (var i = 0; i < count; i++)
            {
                var delayed = _delayLine.Read(_delay);
                var output = data[i] + delayed * Feedback;
                _delayLine.Write(output);
                data[i] = data[i] * (1 - factor) + output * factor;
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