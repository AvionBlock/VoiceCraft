namespace VoiceCraft.Core.Events
{
    public class ComponentUpdatedEvent
    {
        public readonly IEntityComponent Component;

        public ComponentUpdatedEvent(IEntityComponent component) => Component = component;
    }
}