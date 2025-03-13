namespace VoiceCraft.Core.Events
{
    public struct ComponentRemovedEvent
    {
        public readonly IEntityComponent Component;
        
        public ComponentRemovedEvent(IEntityComponent component) => Component = component;
    }
}