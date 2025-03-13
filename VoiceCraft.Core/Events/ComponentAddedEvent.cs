namespace VoiceCraft.Core.Events
{
    public struct ComponentAddedEvent
    {
        public readonly IEntityComponent Component;

        public ComponentAddedEvent(IEntityComponent component) => Component = component;
    }
}