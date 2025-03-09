namespace VoiceCraft.Core.Events
{
    public class ComponentUpdatedEvent
    {
        public readonly object Component;

        public ComponentUpdatedEvent(object component)
        {
            Component = component;
        }
    }
}