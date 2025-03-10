using System;
using Arch.Core;

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
        Entity Entity { get; }
        
        event Action? OnDestroyed;
    }
}