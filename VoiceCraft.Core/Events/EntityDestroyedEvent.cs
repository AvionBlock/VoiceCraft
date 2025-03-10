using Arch.Core;

namespace VoiceCraft.Core.Events
{
    public class EntityDestroyedEvent
    {
        public readonly Entity Entity;
        
        public EntityDestroyedEvent(Entity entity) => Entity = entity;
    }
}