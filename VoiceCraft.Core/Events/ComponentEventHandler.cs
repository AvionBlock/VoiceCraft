using System;
using Arch.Bus;

namespace VoiceCraft.Core.Events
{
    public partial class ComponentEventHandler
    {
        public event Action<object>? OnComponentUpdated;

        protected ComponentEventHandler()
        {
            Hook();
        }

        [Event(0)]
        public virtual void ComponentUpdated(ref ComponentUpdatedEvent @event)
        {
            OnComponentUpdated?.Invoke(@event.Component);
        }
    }
}