using System;
using System.Collections.Generic;
using System.Linq;
using Arch.Core;
using Arch.Core.Extensions;
using LiteNetLib;
using VoiceCraft.Core.Events;

namespace VoiceCraft.Core.Components
{
    public class NetworkComponent : IEntityComponent
    {
        private List<NetworkComponent> _visibleNetworkEntities = new List<NetworkComponent>();
        private bool _isDisposed;
        private bool IsAlive => !_isDisposed && Entity.IsAlive();

        public Entity Entity { get; }

        public event Action? OnDestroyed;

        public int NetworkId { get; }

        public NetPeer? NetPeer { get; }

        public IEnumerable<NetworkComponent> VisibleNetworkEntities => _visibleNetworkEntities;

        public NetworkComponent(Entity entity, int networkId, NetPeer? netPeer)
        {
            if (entity.Has<NetworkComponent>())
                throw new InvalidOperationException($"Entity already has the {GetType().Name}!");
            Entity = entity;
            NetworkId = networkId;
            NetPeer = netPeer;
            Entity.Add(this);
            WorldEventHandler.InvokeComponentAdded(new ComponentAddedEvent(this));
        }

        public void AddVisibleEntity(NetworkComponent networkComponent)
        {
            if (!networkComponent.IsAlive || networkComponent == this) return;
            networkComponent.OnDestroyed += ClearDeadVisibleEntities;
            _visibleNetworkEntities.Add(networkComponent);
        }

        public bool RemoveVisibleEntity(NetworkComponent networkComponent)
        {
            networkComponent.OnDestroyed -= ClearDeadVisibleEntities;
            var removed = _visibleNetworkEntities.Remove(networkComponent);
            return removed;
        }

        public void SetVisibleEntities(List<NetworkComponent> visibleNetworkEntities)
        {
            foreach (var networkComponent in _visibleNetworkEntities)
                networkComponent.OnDestroyed -= ClearDeadVisibleEntities;
            
            _visibleNetworkEntities = visibleNetworkEntities;
            _visibleNetworkEntities.Remove(this);

            foreach (var networkComponent in _visibleNetworkEntities.Where(networkComponent => networkComponent.IsAlive))
                networkComponent.OnDestroyed += ClearDeadVisibleEntities;
        }

    public void ClearDeadVisibleEntities()
        {
            foreach (var networkComponent in _visibleNetworkEntities.ToList().Where(networkComponent => !networkComponent.IsAlive))
                RemoveVisibleEntity(networkComponent);
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