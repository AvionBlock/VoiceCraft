using System;
using System.Numerics;
using LiteNetLib.Utils;
using VoiceCraft.Core.Interfaces;

namespace VoiceCraft.Core.Audio.Effects
{
    public class ProximityEffect : IAudioEffect, IVisible
    {
        public int MinRange { get; set; }
        public int MaxRange { get; set; }
        public EffectType EffectType => EffectType.Proximity;

        public virtual void Process(VoiceCraftEntity from, VoiceCraftEntity to, uint effectBitmask, Span<float> data,
            int count)
        {
            throw new NotSupportedException();
        }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(MinRange);
            writer.Put(MaxRange);
        }

        public void Deserialize(NetDataReader reader)
        {
            MinRange = reader.GetInt();
            MaxRange = reader.GetInt();
        }

        public void Dispose()
        {
            //Nothing to dispose.
        }


        public bool Visibility(VoiceCraftEntity from, VoiceCraftEntity to, uint effectBitmask)
        {
            var bitmask = from.TalkBitmask & to.ListenBitmask & from.EffectBitmask & to.EffectBitmask;
            if ((bitmask & effectBitmask) == 0) return true; //Proximity checking disabled.
            var distance = Vector3.Distance(from.Position, to.Position);
            return distance <= MaxRange;
        }
    }
}