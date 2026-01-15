using System;
using System.Collections.Generic;
using LiteNetLib.Utils;
using VoiceCraft.Core;
using VoiceCraft.Core.Audio;
using VoiceCraft.Core.World;
using VoiceCraft.Network.Interfaces;

namespace VoiceCraft.Network.Audio.Effects
{
    public class DirectionalEffect : IAudioEffect
    {
        private readonly Dictionary<VoiceCraftEntity, LerpSampleDirectionalVolume> _lerpSampleDirectionalVolumes = new();

        private float _wetDry = 1.0f;

        public static int SampleRate => Constants.SampleRate;

        public float WetDry
        {
            get => _wetDry;
            set => _wetDry = Math.Clamp(value, 0.0f, 1.0f);
        }

        public EffectType EffectType => EffectType.Directional;

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(WetDry);
        }

        public void Deserialize(NetDataReader reader)
        {
            WetDry = reader.GetFloat();
        }

        public void Process(VoiceCraftEntity from, VoiceCraftEntity to, ushort effectBitmask, Span<float> data,
            int count)
        {
            var bitmask = from.TalkBitmask & to.ListenBitmask & from.EffectBitmask & to.EffectBitmask;
            if ((bitmask & effectBitmask) == 0) return; //Not enabled.

            var rot = (float)(Math.Atan2(to.Position.Z - from.Position.Z, to.Position.X - from.Position.X) -
                              to.Rotation.Y * Math.PI / 180);
            var left = (float)Math.Max(0.5 - Math.Cos(rot) * 0.5, 0.2);
            var right = (float)Math.Max(0.5 + Math.Cos(rot) * 0.5, 0.2);

            var lerpSampleDirectionalVolume = GetOrCreateLerpSampleDirectionalVolume(from);
            lerpSampleDirectionalVolume.SetVolumes(left, right);

            for (var i = 0; i < count; i += 2)
            {
                var output = lerpSampleDirectionalVolume.Transform(data[i], data[i + 1]);

                data[i] = output.Item1 * WetDry + data[i] * (1.0f - WetDry);
                data[i + 1] = output.Item2 * WetDry + data[i + 1] * (1.0f - WetDry);
                lerpSampleDirectionalVolume.Step();
            }
        }

        public void Reset()
        {
            //Nothing to reset
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        private LerpSampleDirectionalVolume GetOrCreateLerpSampleDirectionalVolume(VoiceCraftEntity entity)
        {
            lock (_lerpSampleDirectionalVolumes)
            {
                if (_lerpSampleDirectionalVolumes.TryGetValue(entity, out var lerpSampleDirectionalVolume))
                    return lerpSampleDirectionalVolume;
                lerpSampleDirectionalVolume =
                    new LerpSampleDirectionalVolume(SampleRate, TimeSpan.FromMilliseconds(20));
                _lerpSampleDirectionalVolumes.TryAdd(entity, lerpSampleDirectionalVolume);
                entity.OnDestroyed += RemoveLerpSampleDirectionalVolume;
                return lerpSampleDirectionalVolume;
            }
        }

        private void RemoveLerpSampleDirectionalVolume(VoiceCraftEntity entity)
        {
            lock (_lerpSampleDirectionalVolumes)
            {
                _lerpSampleDirectionalVolumes.Remove(entity);
                entity.OnDestroyed -= RemoveLerpSampleDirectionalVolume;
            }
        }

        private class LerpSampleDirectionalVolume(int sampleRate, TimeSpan duration)
        {
            private readonly SampleLerpVolume _channel1 = new(sampleRate, duration);
            private readonly SampleLerpVolume _channel2 = new(sampleRate, duration);

            public void SetVolumes(float channel1, float channel2)
            {
                _channel1.TargetVolume = channel1;
                _channel2.TargetVolume = channel2;
            }

            public (float, float) Transform(float sample1, float sample2)
            {
                return (_channel1.Transform(sample1), _channel2.Transform(sample2));
            }

            //Step forward 1 sample.
            public void Step()
            {
                _channel1.Step();
                _channel2.Step();
            }
        }
    }
}