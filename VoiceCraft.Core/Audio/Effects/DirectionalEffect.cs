using System;
using LiteNetLib.Utils;
using VoiceCraft.Core.Interfaces;
using VoiceCraft.Core.World;

namespace VoiceCraft.Core.Audio.Effects
{
    public class DirectionalEffect : IAudioEffect
    {
        public EffectType EffectType => EffectType.Directional;
        
        public void Serialize(NetDataWriter writer)
        {
        }

        public void Deserialize(NetDataReader reader)
        {
        }

        public virtual void Process(VoiceCraftEntity from, VoiceCraftEntity to, uint effectBitmask, Span<float> data,
            int count)
        {
            var bitmask = from.TalkBitmask & to.ListenBitmask & from.EffectBitmask & to.EffectBitmask;
            if ((bitmask & effectBitmask) == 0) return; //Not enabled.

            var rot = (float)(Math.Atan2(to.Position.Z - from.Position.Z, to.Position.X - from.Position.X) -
                              to.Rotation.X * Math.PI / 180);
            var right = (float)Math.Max(0.5 + Math.Cos(rot) * 0.5, 0.2);
            var left = (float)Math.Max(0.5 - Math.Cos(rot) * 0.5, 0.2);

            for (var i = 0; i < count; i += 2)
            {
                data[i] *= left;
                data[i + 1] *= right;
            }
        }

        public void Reset()
        {
            //Nothing to reset
        }

        public void Dispose()
        {
            //Nothing to dispose.
        }
    }
}