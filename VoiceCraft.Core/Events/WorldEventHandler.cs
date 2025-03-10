using System;
using Arch.Bus;

namespace VoiceCraft.Core.Events
{
    public static class WorldEventHandler
    {
        public static event Action<EntityCreatedEvent>? OnEntityCreated;
        public static event Action<ComponentAddedEvent>? OnComponentAdded;
        public static event Action<ComponentUpdatedEvent>? OnComponentUpdated;
        public static event Action<ComponentRemovedEvent>? OnComponentRemoved;
        public static event Action<EntityDestroyedEvent>? OnEntityDestroyed;

        [Event(0)]
        public static void EntityCreated(ref EntityCreatedEvent @event)
        {
            OnEntityCreated?.Invoke(@event);
        }

        [Event(1)]
        public static void ComponentAdded(ref ComponentAddedEvent @event)
        {
            OnComponentAdded?.Invoke(@event);
        }

        [Event(2)]
        public static void ComponentUpdated(ref ComponentUpdatedEvent @event)
        {
            OnComponentUpdated?.Invoke(@event);
        }

        [Event(3)]
        public static void ComponentRemoved(ref ComponentRemovedEvent @event)
        {
            OnComponentRemoved?.Invoke(@event);
        }

        [Event(4)]
        public static void EntityDestroyed(ref EntityDestroyedEvent @event)
        {
            OnEntityDestroyed?.Invoke(@event);
        }

        public static void InvokeEntityCreated(EntityCreatedEvent @event)
        {
            EventBus.Send(ref @event);
        }

        public static void InvokeComponentAdded(ComponentAddedEvent @event)
        {
            EventBus.Send(ref @event);
        }

        public static void InvokeComponentUpdated(ComponentUpdatedEvent @event)
        {
            EventBus.Send(ref @event);
        }

        public static void InvokeComponentRemoved(ComponentRemovedEvent @event)
        {
            EventBus.Send(ref @event);
        }

        public static void InvokeEntityDestroyed(EntityDestroyedEvent @event)
        {
            EventBus.Send(ref @event);
        } 
    }
}