using System;
using LiteNetLib.Utils;
using VoiceCraft.Core.Network;

namespace VoiceCraft.Core.Interfaces
{
    public interface IAudioEffect : INetSerializable
    {
        public ulong Bitmask { get; }
        
        EffectType EffectType { get; }
    }
}