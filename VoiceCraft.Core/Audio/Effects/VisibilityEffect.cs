using System;
using LiteNetLib.Utils;
using VoiceCraft.Core.Interfaces;

namespace VoiceCraft.Core.Audio.Effects
{
    public class VisibilityEffect : IAudioEffect, IVisible
    {
        public EffectType EffectType => EffectType.Visibility;

        public virtual void Process(VoiceCraftEntity from, VoiceCraftEntity to, uint effectBitmask, Span<float> data,
            int count)
        {
        }

        public void Serialize(NetDataWriter writer)
        {
        }

        public void Deserialize(NetDataReader reader)
        {
        }

        public bool Visibility(VoiceCraftEntity from, VoiceCraftEntity to, uint effectBitmask)
        {
            var bitmask = from.TalkBitmask & to.ListenBitmask & from.EffectBitmask & to.EffectBitmask;
            if ((effectBitmask & bitmask) == 0) return true; //Disabled, is visible by default.
            
            return !string.IsNullOrWhiteSpace(from.WorldId) && !string.IsNullOrWhiteSpace(to.WorldId) && 
                   from.WorldId == to.WorldId;
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