using System;
using LiteNetLib.Utils;

namespace VoiceCraft.Core.Interfaces
{
    public interface IAudioEffect : INetSerializable, IDisposable
    {
        public ulong Bitmask { get; }
        
        EffectType EffectType { get; }

        void Process(VoiceCraftEntity from, VoiceCraftEntity to, Span<float> data, int count);
    }
}