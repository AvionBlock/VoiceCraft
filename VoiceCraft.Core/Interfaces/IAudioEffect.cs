using System;
using LiteNetLib.Utils;

namespace VoiceCraft.Core.Interfaces
{
    public interface IAudioEffect : INetSerializable
    {
        event Action<IAudioEffect>? OnEffectUpdated;
        
        public bool VisibleTo(VoiceCraftEntity entity1, VoiceCraftEntity entity2);
    }
}