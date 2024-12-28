using System;
using Arch.Core;

namespace VoiceCraft.Core.Interfaces
{
    public interface IComponent<out T> where T : class
    {
        event Action<T> OnUpdate;
        event Action<T> OnDestroy;
        Guid Id { get; }
        World World { get; }
        Entity Entity { get; }
        
        void Destroy();
    }
}