using System;

namespace VoiceCraft.Core.Events
{
    public static class WorldEventHandler
    {
        public static event Action<EntityCreatedEvent>? OnEntityCreated;
        public static event Action<ComponentAddedEvent>? OnComponentAdded;
        public static event Action<ComponentUpdatedEvent>? OnComponentUpdated;
        public static event Action<ComponentRemovedEvent>? OnComponentRemoved;
        public static event Action<EntityDestroyedEvent>? OnEntityDestroyed;

        public static void InvokeEntityCreated(in EntityCreatedEvent @event)
        {
            OnEntityCreated?.Invoke(@event);
        }

        public static void InvokeComponentAdded(in ComponentAddedEvent @event)
        {
            OnComponentAdded?.Invoke(@event);
        }

        public static void InvokeComponentUpdated(in ComponentUpdatedEvent @event)
        {
            OnComponentUpdated?.Invoke(@event);
        }

        public static void InvokeComponentRemoved(in ComponentRemovedEvent @event)
        {
            OnComponentRemoved?.Invoke(@event);
        }

        public static void InvokeEntityDestroyed(in EntityDestroyedEvent @event)
        {
            OnEntityDestroyed?.Invoke(@event);
        }
    }
}