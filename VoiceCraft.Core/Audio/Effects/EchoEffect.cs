using System;
using System.Collections.Generic;
using LiteNetLib.Utils;
using VoiceCraft.Core.Interfaces;
using VoiceCraft.Core.World;

namespace VoiceCraft.Core.Audio.Effects
{
    public class EchoEffect : IAudioEffect
    {
        private readonly Dictionary<VoiceCraftEntity, FractionalDelayLine> _delayLines =
            new Dictionary<VoiceCraftEntity, FractionalDelayLine>();
        private float _delay;

        public EchoEffect(float delay, float feedback = 0.5f)
        {
            Delay = delay;
            Feedback = feedback;
        }

        public int SampleRate { get; } = Constants.SampleRate;
        public float Delay
        {
            get => _delay / SampleRate;
            set => _delay = SampleRate * value;
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

        public void Process(VoiceCraftEntity from, VoiceCraftEntity to, ushort effectBitmask, Span<float> data, int count)
        {
            var bitmask = from.TalkBitmask & to.ListenBitmask & from.EffectBitmask & to.EffectBitmask;
            if ((bitmask & effectBitmask) == 0)
                return; //There may still be echo from the entity itself but that will phase out over time.

            var delayLine = GetOrCreateDelayLine(from);
            delayLine.Ensure(SampleRate, Delay);

            for (var i = 0; i < count; i++)
            {
                var delayed = delayLine.Read(_delay);
                var output = data[i] + delayed * Feedback;
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