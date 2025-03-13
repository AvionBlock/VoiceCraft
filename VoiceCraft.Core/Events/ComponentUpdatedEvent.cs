namespace VoiceCraft.Core.Events
{
    public struct ComponentUpdatedEvent
    {
        public readonly IEntityComponent Component;

        public ComponentUpdatedEvent(IEntityComponent component) => Component = component;
    }
}