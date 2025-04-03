using System;
using LiteNetLib.Utils;
using VoiceCraft.Core.Network;

namespace VoiceCraft.Core.Interfaces
{
    public interface IAudioEffect : INetSerializable
    {
        public ulong Bitmask { get; }
        
        EffectType EffectType { get; }
        
        bool ApplyEffect(byte[] data, uint sampleRate, uint channels, VoiceCraftEntity fromEntity, VoiceCraftEntity toEntity);
    }
}