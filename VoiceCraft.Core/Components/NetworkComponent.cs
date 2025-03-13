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
        private List<Entity> _visibleEntities = new List<Entity>();
        private bool _isDisposed;

        public Entity Entity { get; }

        public event Action? OnDestroyed;

        public int NetworkId { get; }

        public NetPeer? NetPeer { get; }

        public IEnumerable<Entity> VisibleEntities => _visibleEntities;

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

        public void SetVisibleEntities(List<Entity> visibleEntities)
        {
            foreach (var visibleEntity in visibleEntities)
            {
                //Already is visible or does not have a network component.
                if(_visibleEntities.Contains(visibleEntity) || !visibleEntity.Has<NetworkComponent>()) continue;
                var networkComponent = visibleEntity.Get<NetworkComponent>();
                networkComponent.OnDestroyed += ClearDeadEntities;
                _visibleEntities.Add(visibleEntity);
            }

            foreach (var visibleEntity in _visibleEntities.Where(visibleEntity => !visibleEntities.Contains(visibleEntity)))
            {
                visibleEntity.TryGet<NetworkComponent>(out var networkComponent); //May not contain it so we do a safe event removal.
                if(networkComponent != null)
                    networkComponent.OnDestroyed -= ClearDeadEntities;
                _visibleEntities.Remove(visibleEntity);
            }
        }

        public void ClearDeadEntities()
        {
            for(var i = _visibleEntities.Count - 1; i >= 0; i--) //Reverse indexing to remove dead entities.
            {
                var visibleEntity = _visibleEntities[i];
                if(visibleEntity.IsAlive() && visibleEntity.Has<NetworkComponent>()) continue; //Alive and working.
                
                //Dead, Remove it.
                _visibleEntities.RemoveAt(i);
            }
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