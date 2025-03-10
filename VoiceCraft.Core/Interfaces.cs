using System;
using Arch.Core;
using VoiceCraft.Core.Network;

namespace VoiceCraft.Core
{
    public interface IAudioEffect
    {
        public uint Bitmask { get; set; }
    }

    public interface IAudioInput
    {
    }

    public interface IAudioOutput
    {
    }

    public interface IEntityComponent: IDisposable
    {
        ComponentType ComponentType { get; }
        
        Entity Entity { get; }
        
        event Action? OnDestroyed;
    }
}