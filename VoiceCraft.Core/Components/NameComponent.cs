using System;
using Arch.Core;
using Arch.Core.Extensions;
using LiteNetLib.Utils;
using VoiceCraft.Core.Events;
using VoiceCraft.Core.Network;

namespace VoiceCraft.Core.Components
{
    public class NameComponent : ISerializableEntityComponent
    {
        private string _name = string.Empty;
        private bool _isDisposed;
        private bool IsAlive => !_isDisposed && Entity.IsAlive();
        
        public ComponentType ComponentType => ComponentType.Name;
        
        public Entity Entity { get; }
        
        public event Action? OnDestroyed;
        
        public string Name
        {
            get => _name;
            set
            {
                if (_name == value || !IsAlive) return;
                _name = value;
                WorldEventHandler.InvokeComponentUpdated(new ComponentUpdatedEvent(this));
            }
        }

        public NameComponent(Entity entity)
        {
            if (entity.Has<NameComponent>())
                throw new InvalidOperationException($"Entity already has the {GetType().Name}!");
            Entity = entity;
            Entity.Add(this);
            WorldEventHandler.InvokeComponentAdded(new ComponentAddedEvent(this));
        }
        
        public void Serialize(NetDataWriter writer)
        {
            writer.Put(_name);
        }

        public void Deserialize(NetDataReader reader)
        {
            _name = reader.GetString();
        }
        
        public void Dispose()
        {
            if (_isDisposed) return;
            Entity.Remove<NameComponent>();
            _isDisposed = true;
            OnDestroyed?.Invoke();
            WorldEventHandler.InvokeComponentRemoved(new ComponentRemovedEvent(this));
        }
    }
}