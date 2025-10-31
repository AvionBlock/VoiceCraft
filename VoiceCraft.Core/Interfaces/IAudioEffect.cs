using System;
using LiteNetLib.Utils;
using VoiceCraft.Core.World;

namespace VoiceCraft.Core.Interfaces
{
    public interface IAudioEffect : INetSerializable, IDisposable
    {
        EffectType EffectType { get; }

        void Process(VoiceCraftEntity from, VoiceCraftEntity to, ushort effectBitmask, Span<float> data, int count);

        void Reset();
    }
}