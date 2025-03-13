using System;
using System.Collections.Generic;
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

    public interface ISerializableEntityComponent : IEntityComponent, INetSerializable
    {
        ComponentType ComponentType { get; }
    }
    
    public interface IVisibleComponent : IVisibilityComponent
    {
        public void GetVisibleEntities(World world, List<Entity> entities);
    }

    public interface IVisibilityComponent
    {
        bool VisibleTo(Entity entity);
    }
}