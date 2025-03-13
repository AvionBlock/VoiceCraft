using Arch.Core;

namespace VoiceCraft.Core.Events
{
    public struct EntityDestroyedEvent
    {
        public readonly Entity Entity;
        
        public EntityDestroyedEvent(Entity entity) => Entity = entity;
    }
}