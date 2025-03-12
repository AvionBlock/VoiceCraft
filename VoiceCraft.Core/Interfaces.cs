using System;
using System.Collections.Generic;
using Arch.Core;
using VoiceCraft.Core.Network;

namespace VoiceCraft.Core
{
    public interface IAudioEffect
    {
        public ulong Bitmask { get; set; }
    }

    public interface IAudioInput : IVisibleComponent
    {
    }

    public interface IAudioOutput : IVisibleComponent
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
    
    public interface IVisibleComponent
    {
        public void GetVisibleComponents(World world, List<object> components);
    }

    public interface IVisibilityComponent
    {
        bool VisibleTo(Entity entity, ulong bitmask);
    }
}