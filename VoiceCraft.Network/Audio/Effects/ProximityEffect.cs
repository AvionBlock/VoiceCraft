using System;
using System.Collections.Generic;
using System.Numerics;
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
        private readonly Dictionary<VoiceCraftEntity, SampleLerpVolume> _lerpSampleVolumes = new();
        private float _wetDry = 1.0f;

        public static int SampleRate => Constants.SampleRate;
        
        public float WetDry
        {
            get => _wetDry;
            set => _wetDry = Math.Clamp(value, 0.0f, 1.0f);
        }
        public int MinRange { get; set; }
        public int MaxRange { get; set; }

        public EffectType EffectType => EffectType.Proximity;

        public void Process(VoiceCraftEntity from, VoiceCraftEntity to, ushort effectBitmask, Span<float> data,
            int count)
        {
            var bitmask = from.TalkBitmask & to.ListenBitmask & from.EffectBitmask & to.EffectBitmask;
            if ((bitmask & effectBitmask) == 0) return; //Not enabled.

            var range = MaxRange - MinRange;
            if (range == 0) return; //Range is 0. Do not calculate division.
            var distance = Vector3.Distance(from.Position, to.Position);
            var factor = 1f - Math.Clamp((distance - MinRange) / range, 0f, 1f);
            
            var lerpVolumeSample = GetOrCreateLerpSampleVolume(from);
            lerpVolumeSample.TargetVolume = factor;

            for (var i = 0; i < count; i += 2)
            {
                //Channel 1
                var output = lerpVolumeSample.Transform(data[i]);
                data[i] = output * WetDry + data[i] * (1.0f - WetDry);
                //Channel 2
                output = lerpVolumeSample.Transform(data[i + 1]);
                data[i + 1] = output * WetDry + data[i + 1] * (1.0f - WetDry);
                lerpVolumeSample.Step();
            }
        }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(MinRange);
            writer.Put(MaxRange);
            writer.Put(WetDry);
        }

        public void Deserialize(NetDataReader reader)
        {
            MinRange = reader.GetInt();
            MaxRange = reader.GetInt();
            WetDry = reader.GetFloat();
        }

        public void Reset()
        {
            //Nothing to reset
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        public bool Visibility(VoiceCraftEntity from, VoiceCraftEntity to, ushort effectBitmask)
        {
            var bitmask = from.TalkBitmask & to.ListenBitmask & from.EffectBitmask & to.EffectBitmask;
            if ((bitmask & effectBitmask) == 0) return true; //Proximity checking disabled.
            var distance = Vector3.Distance(from.Position, to.Position);
            return distance <= MaxRange;
        }
        
        private SampleLerpVolume GetOrCreateLerpSampleVolume(VoiceCraftEntity entity)
        {
            lock (_lerpSampleVolumes)
            {
                if (_lerpSampleVolumes.TryGetValue(entity, out var lerpSampleVolume))
                    return lerpSampleVolume;
                lerpSampleVolume = new SampleLerpVolume(SampleRate, TimeSpan.FromMilliseconds(20));
                _lerpSampleVolumes.TryAdd(entity, lerpSampleVolume);
                entity.OnDestroyed += RemoveLerpSampleVolume;
                return lerpSampleVolume;
            }
        }
        
        private void RemoveLerpSampleVolume(VoiceCraftEntity entity)
        {
            lock (_lerpSampleVolumes)
            {
                _lerpSampleVolumes.Remove(entity);
                entity.OnDestroyed -= RemoveLerpSampleVolume;
            }
        }
    }
}