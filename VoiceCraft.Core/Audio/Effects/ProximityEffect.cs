using System;
using System.Numerics;
using LiteNetLib.Utils;
using VoiceCraft.Core.Interfaces;
using VoiceCraft.Core.World;

namespace VoiceCraft.Core.Audio.Effects
{
    public class ProximityEffect : IAudioEffect, IVisible
    {
        private float _wetDry = 1.0f;
        
        public EffectType EffectType => EffectType.Proximity;
        public float WetDry
        {
            get => _wetDry;
            set => _wetDry = Math.Clamp(value, 0.0f, 1.0f);
        }
        public int MinRange { get; set; }
        public int MaxRange { get; set; }

        public virtual void Process(VoiceCraftEntity from, VoiceCraftEntity to, ushort effectBitmask, Span<float> data,
            int count)
        {
            var bitmask = from.TalkBitmask & to.ListenBitmask & from.EffectBitmask & to.EffectBitmask;
            if ((bitmask & effectBitmask) == 0) return; //Not enabled.

            var range = MaxRange - MinRange;
            if (range == 0) return; //Range is 0. Do not calculate division.
            var distance = Vector3.Distance(from.Position, to.Position);
            var factor = 1f - Math.Clamp((distance - MinRange) / range, 0f, 1f);

            for (var i = 0; i < count; i++)
            {
                var output = Math.Clamp(data[i] * factor, -1f, 1f);
                data[i] = output * WetDry + data[i] * (1.0f - WetDry);
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
            //Nothing to dispose.
        }

        public bool Visibility(VoiceCraftEntity from, VoiceCraftEntity to, ushort effectBitmask)
        {
            var bitmask = from.TalkBitmask & to.ListenBitmask & from.EffectBitmask & to.EffectBitmask;
            if ((bitmask & effectBitmask) == 0) return true; //Proximity checking disabled.
            var distance = Vector3.Distance(from.Position, to.Position);
            return distance <= MaxRange;
        }
    }
}