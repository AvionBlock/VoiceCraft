using System;
using Arch.Core;

namespace VoiceCraft.Core.Interfaces
{
    public interface IComponent
    {
        event Action<IComponent> OnUpdate;
        event Action<IComponent> OnDestroy;
        Guid Id { get; }
        World World { get; }
        Entity Entity { get; }
        bool IsVisibleToEntity(Entity otherEntity);
        
        void Destroy();
    }
}