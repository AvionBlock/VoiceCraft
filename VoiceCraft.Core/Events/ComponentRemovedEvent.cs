namespace VoiceCraft.Core.Events
{
    public class ComponentRemovedEvent
    {
        public readonly IEntityComponent Component;
        
        public ComponentRemovedEvent(IEntityComponent component) => Component = component;
    }
}