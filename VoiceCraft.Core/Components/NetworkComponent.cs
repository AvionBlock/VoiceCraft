using System;
using System.Collections.Generic;
using Arch.Core;
using Arch.Core.Extensions;
using LiteNetLib;
using VoiceCraft.Core.Events;
using VoiceCraft.Core.Network;

namespace VoiceCraft.Core.Components
{
    public class NetworkComponent : IEntityComponent
    {
        private bool _isDisposed;
        private bool IsAlive => !_isDisposed && Entity.IsAlive();
        
        public ComponentType ComponentType => ComponentType.Network;
        
        public Entity Entity { get; }
        
        public event Action? OnDestroyed;
        
        public int NetworkId { get; }
        
        public NetPeer? NetPeer { get; }
        
        public List<Entity> VisibleEntities { get; } = new List<Entity>();

        public NetworkComponent(Entity entity, int networkId, NetPeer netPeer)
        {
            if (entity.Has<NetworkComponent>())
                throw new InvalidOperationException($"Entity already has the {GetType().Name}!");
            Entity = entity;
            NetworkId = networkId;
            NetPeer = netPeer;
            Entity.Add(this);
            WorldEventHandler.InvokeComponentAdded(new ComponentAddedEvent(this));
        }
        
        public void Dispose()
        {
            if (_isDisposed) return;
            Entity.Remove<NetworkComponent>();
            _isDisposed = true;
            OnDestroyed?.Invoke();
            WorldEventHandler.InvokeComponentRemoved(new ComponentRemovedEvent(this));
        }
    }
}