using System;
using System.Collections.Generic;
using Arch.Core;
using LiteNetLib.Utils;
using VoiceCraft.Core.Network;

namespace VoiceCraft.Core
{
    //Audio
    public interface IAudioStreamable
    {
        int ReadStream(byte[] buffer, int length, int count);
    }
    
    public interface IAudioInput
    {
        int ReadInput(byte[] buffer, int offset, int count);
    }

    public interface IAudioOutput
    {
        int ReadOutput(byte[] buffer, int offset, int count);
    }
    
    public interface IAudioEffect
    {
        public ulong Bitmask { get; set; }
    }
    
    //Visibility
    public interface IVisibleComponent : IVisibilityComponent
    {
        public void GetVisibleEntities(World world, List<Entity> entities);
    }

    public interface IVisibilityComponent
    {
        bool VisibleTo(Entity entity);
    }

    //Entity
    public interface IEntityComponent: IDisposable
    {
        Entity Entity { get; }
        
        event Action? OnDestroyed;
    }

    public interface ISerializableEntityComponent : IEntityComponent, INetSerializable
    {
        ComponentType ComponentType { get; }
    }
}