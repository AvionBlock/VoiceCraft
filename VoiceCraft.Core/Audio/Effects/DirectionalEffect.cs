using System;
using System.Numerics;
using LiteNetLib.Utils;
using VoiceCraft.Core.Interfaces;

namespace VoiceCraft.Core.Audio.Effects
{
    public class DirectionalEffect : IAudioEffect
    {
        public EffectType EffectType => EffectType.Directional;

        public virtual void Process(VoiceCraftEntity from, VoiceCraftEntity to, uint effectBitmask, Span<float> data,
            int count)
        {
            var rot = (float)(Math.Atan2(to.Position.Z - from.Position.Z, to.Position.X - from.Position.X) -
                    to.Rotation.X * Math.PI / 180);
            throw new NotSupportedException();
        }

        public void Serialize(NetDataWriter writer)
        {
        }

        public void Deserialize(NetDataReader reader)
        {
        }

        public void Dispose()
        {
            //Nothing to dispose.
        }
    }
}