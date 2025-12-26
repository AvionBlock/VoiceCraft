using System;
using System.Collections.Generic;
using LiteNetLib.Utils;
using VoiceCraft.Core.Interfaces;
using VoiceCraft.Core.World;

namespace VoiceCraft.Core.Audio.Effects
{
    public class ProximityMuffleEffect : IAudioEffect
    {
        private readonly Dictionary<VoiceCraftEntity, BiQuadFilter> _biquadFilters =
            new Dictionary<VoiceCraftEntity, BiQuadFilter>();
        
        private float _wetDry = 1.0f;
        public static int SampleRate => Constants.SampleRate;
        public float WetDry
        {
            get => _wetDry;
            set => _wetDry = Math.Clamp(value, 0.0f, 1.0f);
        }
        public EffectType EffectType => EffectType.ProximityMuffle;

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
            if ((bitmask & effectBitmask) == 0)
                return;

            var factor = MathF.Max(from.MuffleFactor, to.MuffleFactor);
            var biQuadFilter = GetOrCreateBiQuadFilter(from);
            biQuadFilter.SetLowPassFilter(SampleRate, 200, 1);

            for (var i = 0; i < count; i++)
            {
                var output = biQuadFilter.Transform(data[i]) * factor + data[i] * (1.0f - factor);
                data[i] = output * WetDry + data[i] * (1.0f - WetDry);
            }
        }

        public void Reset()
        {
            lock (_biquadFilters)
            {
                _biquadFilters.Clear();
            }
        }

        public void Dispose()
        {
            //Nothing to dispose.
        }

        private BiQuadFilter GetOrCreateBiQuadFilter(VoiceCraftEntity entity)
        {
            lock (_biquadFilters)
            {
                if (_biquadFilters.TryGetValue(entity, out var biQuadFilter))
                    return biQuadFilter;
                biQuadFilter = new BiQuadFilter();
                _biquadFilters.TryAdd(entity, biQuadFilter);
                entity.OnDestroyed += RemoveBiQuadFilter;
                return biQuadFilter;
            }
        }

        private void RemoveBiQuadFilter(VoiceCraftEntity entity)
        {
            lock (_biquadFilters)
            {
                _biquadFilters.Remove(entity);
                entity.OnDestroyed -= RemoveBiQuadFilter;
            }
        }
    }
}