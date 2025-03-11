using System;
using Arch.Core;
using LiteNetLib.Utils;
using VoiceCraft.Core.Network;

namespace VoiceCraft.Core
{
    public interface IAudioEffect
    {
        public ulong Bitmask { get; set; }
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

    public interface ISerializableEntityComponent : IEntityComponent
    {
        ComponentType ComponentType { get; }
        
        byte[]? Serialize();
        
        void Deserialize(byte[] data);
    }
}