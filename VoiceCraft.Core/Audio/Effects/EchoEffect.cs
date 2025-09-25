using System;
using LiteNetLib.Utils;
using VoiceCraft.Core.Interfaces;
using VoiceCraft.Core.World;

namespace VoiceCraft.Core.Audio.Effects
{
    public class EchoEffect : IAudioEffect
    {
        private readonly FractionalDelayLine _delayLine;
        private float _delay;

        public EchoEffect(int samplingRate,
            float delay,
            float feedback = 0.5f)
        {
            _delayLine = new FractionalDelayLine(samplingRate, delay, InterpolationMode.Nearest);
            SampleRate = samplingRate;
            Delay = delay;
            Feedback = feedback;
        }

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
        public float Wet { get; set; } = 1f;
        public float Dry { get; set; }
        public EffectType EffectType => EffectType.Echo;

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(Delay);
            writer.Put(Feedback);
            writer.Put(Wet);
            writer.Put(Dry);
        }

        public void Deserialize(NetDataReader reader)
        {
            Delay = reader.GetFloat();
            Feedback = reader.GetFloat();
            Wet = reader.GetFloat();
            Dry = reader.GetFloat();
        }

        public void Process(VoiceCraftEntity from, VoiceCraftEntity to, uint effectBitmask, Span<float> data, int count)
        {
            var bitmask = from.TalkBitmask & to.ListenBitmask & from.EffectBitmask & to.EffectBitmask;
            if ((bitmask & effectBitmask) == 0)
                return; //There may still be echo from the entity itself but that will phase out over time.

            for (var i = 0; i < count; i++)
            {
                var delayed = _delayLine.Read(_delay);
                var output = data[i] + delayed * Feedback;
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