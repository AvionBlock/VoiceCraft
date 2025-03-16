using System;
using LiteNetLib.Utils;
using VoiceCraft.Core.Network;

namespace VoiceCraft.Core.Interfaces
{
    public interface IAudioEffect : INetSerializable
    {
        public ulong Bitmask { get; set; }

        EffectType EffectType { get; }
        
        event Action<IAudioEffect>? OnEffectUpdated;
    }
}