using System;
using LiteNetLib.Utils;

namespace VoiceCraft.Core.Interfaces
{
    public interface IAudioEffect : INetSerializable, IDisposable
    {
        EffectType EffectType { get; }

        public ulong Bitmask { get; }

        void Process(VoiceCraftEntity from, VoiceCraftEntity to, Span<float> data, int count);
    }
}