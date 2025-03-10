namespace VoiceCraft.Core.Events
{
    public class ComponentAddedEvent
    {
        public readonly IEntityComponent Component;

        public ComponentAddedEvent(IEntityComponent component)
        {
            Component = component;
        }
    }
}