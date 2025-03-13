using Arch.Core;

namespace VoiceCraft.Core.Events
{
    public struct EntityCreatedEvent
    {
        public readonly Entity Entity;
        
        public EntityCreatedEvent(Entity entity) => Entity = entity;
    }
}