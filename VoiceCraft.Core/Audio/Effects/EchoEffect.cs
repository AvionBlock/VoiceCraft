using System;
using LiteNetLib.Utils;
using VoiceCraft.Core.Interfaces;

namespace VoiceCraft.Core.Audio.Effects
{
    /// <summary>
    /// Represents Echo audio effect.
    /// </summary>
    public class EchoEffect : IAudioEffect
    {
        public EffectType EffectType => EffectType.Echo;

        /// <summary>
        /// Internal fractional delay line.
        /// </summary>
        private readonly FractionalDelayLine _delayLine;

        /// <summary>
        /// Sampling rate.
        /// </summary>
        public int SampleRate { get; }

        /// <summary>
        /// Gets or sets delay (in seconds).
        /// </summary>
        public float Delay
        {
            get => _delay / SampleRate;
            set
            {
                _delayLine.Ensure(SampleRate, value);
                _delay = SampleRate * value;
            }
        }

        private float _delay;

        /// <summary>
        /// Gets or sets feedback parameter.
        /// </summary>
        public float Feedback { get; set; }

        /// <summary>
        /// Gets or sets wet gain (by default, 1).
        /// </summary>
        public float Wet { get; set; } = 1f;

        /// <summary>
        /// Gets or sets dry gain (by default, 0).
        /// </summary>
        public float Dry { get; set; }

        /// <summary>
        /// Constructs <see cref="EchoEffect"/>.
        /// </summary>
        /// <param name="samplingRate">Sampling rate</param>
        /// <param name="delay">Delay (in seconds)</param>
        /// <param name="feedback">Feedback</param>
        public EchoEffect(int samplingRate,
            float delay,
            float feedback = 0.5f)
        {
            _delayLine = new FractionalDelayLine(samplingRate, delay, InterpolationMode.Nearest);
            SampleRate = samplingRate;
            Delay = delay;
            Feedback = feedback;
        }

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
                data[i] = data[i] * Dry + output * Wet;
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