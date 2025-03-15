using System;
using System.Collections.Generic;
using Arch.Core;
using Arch.Core.Extensions;
using LiteNetLib;
using VoiceCraft.Core.Events;

namespace VoiceCraft.Core.Components
{
    public class NetworkComponent : IEntityComponent
    {
        private static readonly Dictionary<int, NetworkComponent> NetworkComponents = new Dictionary<int, NetworkComponent>();
        public static readonly QueryDescription Query = new QueryDescription().WithAll<NetworkComponent>();
        private readonly List<NetworkComponent> _visibleNetworkComponents = new List<NetworkComponent>();
        private bool _isDisposed;
        private bool IsAlive => !_isDisposed && Entity.IsAlive();

        public Entity Entity { get; }

        public event Action? OnDestroyed;

        public int NetworkId { get; }

        public NetPeer? NetPeer { get; }

        public IEnumerable<NetworkComponent> VisibleNetworkComponents => _visibleNetworkComponents;

        public NetworkComponent(Entity entity, int networkId, NetPeer? netPeer = null)
        {
            if (entity.Has<NetworkComponent>())
                throw new InvalidOperationException($"Entity already has the {GetType().Name}!");
            if (NetworkComponents.ContainsKey(networkId))
                throw new InvalidOperationException($"A network component for the Network Id {networkId} already exists!");
            if (netPeer != null && networkId != netPeer.Id)
                throw new InvalidOperationException($"Input Network Id {networkId} does not match input NetPeer Id {netPeer.Id}!");
            
            Entity = entity;
            NetPeer = netPeer;
            NetworkId = networkId;
            Entity.Add(this);
            NetworkComponents.Add(NetworkId, this);
            WorldEventHandler.InvokeComponentAdded(new ComponentAddedEvent(this));
        }

        public bool AddVisibleNetworkComponent(NetworkComponent component)
        {
            //Already is visible or does not have a network component.
            if (_visibleNetworkComponents.Contains(component) || !component.IsAlive) return false;
            component.OnDestroyed += ClearDeadNetworkComponents;
            _visibleNetworkComponents.Add(component);
            return true;
        }

        public bool RemoveVisibleNetworkComponent(NetworkComponent component)
        {
            component.OnDestroyed -= ClearDeadNetworkComponents;
            return _visibleNetworkComponents.Remove(component);
        }

        public void ClearDeadNetworkComponents()
        {
            for(var i = _visibleNetworkComponents.Count - 1; i >= 0; i--) //Reverse indexing to remove dead entities.
            {
                var component = _visibleNetworkComponents[i];
                if(component.IsAlive) continue; //Alive and working.
                
                //Dead, Remove it.
                component.OnDestroyed -= ClearDeadNetworkComponents;
                _visibleNetworkComponents.RemoveAt(i);
            }
        }

        public void Dispose()
        {
            if (_isDisposed) return;
            Entity.Remove<NetworkComponent>();
            NetworkComponents.Remove(NetworkId);
            _isDisposed = true;
            OnDestroyed?.Invoke();
            WorldEventHandler.InvokeComponentRemoved(new ComponentRemovedEvent(this));
        }

        public static int GetNextAvailableId()
        {
            for (var i = -1; i > int.MinValue; i--)
            {
                if(!NetworkComponents.ContainsKey(i)) return i;
            }

            throw new Exception("Could not generate a networkId for this entity! ID Limit reached!");
        }
        
        public static NetworkComponent? GetNetworkComponentFromId(int networkId)
        {
            return NetworkComponents.GetValueOrDefault(networkId);
        }
    }
}