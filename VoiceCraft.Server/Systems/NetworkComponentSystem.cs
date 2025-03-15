using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using LiteNetLib;
using VoiceCraft.Core;
using VoiceCraft.Core.Components;
using VoiceCraft.Core.Events;
using VoiceCraft.Core.Network.Packets;

namespace VoiceCraft.Server.Systems
{
    public class NetworkComponentSystem : BaseSystem<World, float>
    {
        private readonly VoiceCraftServer _server;
        private readonly List<Entity> _visibleEntities = [];

        public NetworkComponentSystem(World world, VoiceCraftServer server) : base(world)
        {
            _server = server;

            WorldEventHandler.OnComponentAdded += OnComponentAdded;
            WorldEventHandler.OnComponentUpdated += OnComponentUpdated;
            WorldEventHandler.OnComponentRemoved += OnComponentRemoved;
        }

        public override void Update(in float deltaTime)
        {
            World.Query(in NetworkComponent.Query, (ref NetworkComponent networkComponent) =>
            {
                _visibleEntities.Clear();
                networkComponent.ClearDeadNetworkComponents();
                //Add as local entity. Cascading components will detect this and ignore any computations for the added entity.
                _visibleEntities.Add(networkComponent.Entity); 
                var components = networkComponent.Entity.GetAllComponents();
                foreach (var component in components)
                {
                    if (component is not IVisibleComponent visibleComponent) continue;
                    visibleComponent.GetVisibleEntities(World, _visibleEntities);
                }
                _visibleEntities.Remove(networkComponent.Entity); //Remove local entity.
                SetVisibleEntities(networkComponent, _visibleEntities.Where(x => x.Has<NetworkComponent>()).Select(x => x.Get<NetworkComponent>()).ToArray());
            });
        }

        private void OnComponentAdded(ComponentAddedEvent @event)
        {
            if (@event.Component is NetworkComponent networkComponent)
            {
                var entityCreatedPacket = new EntityCreatedPacket() { NetworkId = networkComponent.NetworkId };
                var componentAddedPacket = new AddComponentPacket() { NetworkId = networkComponent.NetworkId };
                var components = @event.Component.Entity.GetAllComponents();

                Broadcast(entityCreatedPacket, networkComponent.NetPeer);
                foreach (var component in components)
                {
                    if (component is not ISerializableEntityComponent serializableEntityComponent) continue;
                    componentAddedPacket.ComponentType = serializableEntityComponent.ComponentType;
                    Broadcast(componentAddedPacket);
                }
            }
            else if (@event.Component.Entity.Has<NetworkComponent>() && @event.Component is ISerializableEntityComponent serializableEntityComponent)
            {
                var entityNetworkComponent = @event.Component.Entity.Get<NetworkComponent>();
                var componentAddedPacket = new AddComponentPacket()
                {
                    NetworkId = entityNetworkComponent.NetworkId,
                    ComponentType = serializableEntityComponent.ComponentType
                };
                //Notify all entities of the changes.
                Broadcast(componentAddedPacket);
            }
        }

        private void OnComponentUpdated(ComponentUpdatedEvent @event)
        {
            if (!@event.Component.Entity.Has<NetworkComponent>() || @event.Component is not ISerializableEntityComponent serializableEntityComponent) return;
            var networkComponent = @event.Component.Entity.Get<NetworkComponent>();
            var componentUpdatedPacket = new UpdateComponentPacket()
            {
                NetworkId = networkComponent.NetworkId,
                Component = serializableEntityComponent
            };
            foreach (var visibleNetworkComponent in networkComponent.VisibleNetworkComponents)
            {
                if (visibleNetworkComponent.NetPeer == null) continue;
                _server.SendPacket(visibleNetworkComponent.NetPeer, componentUpdatedPacket);
            }
        }

        private void OnComponentRemoved(ComponentRemovedEvent @event)
        {
            if (@event.Component is NetworkComponent networkComponent)
            {
                //Disconnect local client. This should only happen when the server destroys the entity or for some reason. I decide to just allow removal of the
                //network component BECAUSE WHY THE FUCK NOT?
                networkComponent.NetPeer?.Disconnect();

                var entityDestroyedPacket = new EntityDestroyedPacket() { NetworkId = networkComponent.NetworkId };
                Broadcast(entityDestroyedPacket);
            }
            else if (@event.Component.Entity.Has<NetworkComponent>() && @event.Component is ISerializableEntityComponent serializableEntityComponent)
            {
                var entityNetworkComponent = @event.Component.Entity.Get<NetworkComponent>();
                var componentRemovedPacket = new RemoveComponentPacket()
                {
                    NetworkId = entityNetworkComponent.NetworkId,
                    ComponentType = serializableEntityComponent.ComponentType
                };
                //Notify all entities of the changes.
                Broadcast(componentRemovedPacket);
            }
        }

        private void SetVisibleEntities(NetworkComponent networkComponent, NetworkComponent[] visibleNetworkComponents)
        {
            foreach (var visibleNetworkComponent in visibleNetworkComponents)
            {
                if(!networkComponent.AddVisibleNetworkComponent(visibleNetworkComponent) || networkComponent.NetPeer == null) continue;
                
                //If it's an actual client. Send all the current component values of the newly added entity.
                SendEntityComponentData(networkComponent, visibleNetworkComponent.Entity);
            }
                
            foreach (var visibleNetworkComponent in networkComponent.VisibleNetworkComponents.Where(visible => !visibleNetworkComponents.Contains(visible)))
            {
                networkComponent.RemoveVisibleNetworkComponent(visibleNetworkComponent);
            }
        }

        private void SendEntityComponentData(NetworkComponent networkComponent, Entity targetEntity)
        {
            if (networkComponent.NetPeer == null) return;
            
            var entityComponents = targetEntity.GetAllComponents();
            foreach (var component in entityComponents)
            {
                if (component is not ISerializableEntityComponent serializableEntityComponent) continue;
                var componentUpdatedPacket = new UpdateComponentPacket()
                {
                    NetworkId = networkComponent.NetworkId,
                    Component = serializableEntityComponent
                };
                _server.SendPacket(networkComponent.NetPeer, componentUpdatedPacket);
            }
        }

        private void Broadcast(VoiceCraftPacket packet, params NetPeer?[] excludedPeers)
        {
            World.Query(in NetworkComponent.Query, (ref NetworkComponent netComponent) =>
            {
                if (netComponent.NetPeer == null || excludedPeers.Contains(netComponent.NetPeer)) return; //not a client or is excluded.
                _server.SendPacket(netComponent.NetPeer, packet);
            });
        }
    }
}