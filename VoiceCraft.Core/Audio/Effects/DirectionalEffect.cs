using System;
using LiteNetLib.Utils;
using VoiceCraft.Core.Interfaces;
using VoiceCraft.Core.World;

namespace VoiceCraft.Core.Audio.Effects
{
    public class DirectionalEffect : IAudioEffect
    {
        private float _wetDry = 1.0f;

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

        public virtual void Process(VoiceCraftEntity from, VoiceCraftEntity to, ushort effectBitmask, Span<float> data,
            int count)
        {
            var bitmask = from.TalkBitmask & to.ListenBitmask & from.EffectBitmask & to.EffectBitmask;
            if ((bitmask & effectBitmask) == 0) return; //Not enabled.

            var rot = (float)(Math.Atan2(to.Position.Z - from.Position.Z, to.Position.X - from.Position.X) -
                              to.Rotation.Y * Math.PI / 180);
            var left = (float)Math.Max(0.5 - Math.Cos(rot) * 0.5, 0.2);
            var right = (float)Math.Max(0.5 + Math.Cos(rot) * 0.5, 0.2);

            for (var i = 0; i < count; i += 2)
            {
                var outputLeft = data[i] * left;
                var outputRight = data[i + 1] * right;

                data[i] = outputLeft * WetDry + data[i] * (1.0f - WetDry);
                data[i + 1] = outputRight * WetDry + data[i + 1] * (1.0f - WetDry);
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