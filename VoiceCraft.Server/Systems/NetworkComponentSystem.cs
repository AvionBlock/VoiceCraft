using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using VoiceCraft.Core;
using VoiceCraft.Core.Components;
using VoiceCraft.Core.Events;
using VoiceCraft.Core.Network.Packets;

namespace VoiceCraft.Server.Systems
{
    public class NetworkComponentSystem : BaseSystem<World, float>
    {
        private readonly VoiceCraftServer _server;

        public NetworkComponentSystem(World world, VoiceCraftServer server) : base(world)
        {
            _server = server;

            WorldEventHandler.OnComponentAdded += OnComponentAdded;
            WorldEventHandler.OnComponentRemoved += OnComponentRemoved;
        }

        [Query]
        [All(typeof(NetworkComponent))]
        public void CalculateVisibleEntities([Data] in float deltaTime, ref Entity entity, ref NetworkComponent networkComponent)
        {
            Console.WriteLine(entity);
        }

        private void OnComponentAdded(ComponentAddedEvent @event)
        {
            var query = new QueryDescription()
                .WithAll<NetworkComponent>();
            if (@event.Component is NetworkComponent networkComponent)
            {
                var entityCreatedPacket = new EntityCreatedPacket() { NetworkId = networkComponent.NetworkId };
                var componentAddedPacket = new AddComponentPacket() { NetworkId = networkComponent.NetworkId };
                var components = @event.Component.Entity.GetAllComponents();

                World.Query(in query, (ref Entity entity, ref NetworkComponent entityNetworkComponent) =>
                {
                    if (entity == networkComponent.Entity || entityNetworkComponent.NetPeer == null) return; //Local entity or not a client.
                    _server.SendPacket(entityNetworkComponent.NetPeer, entityCreatedPacket);
                    foreach (var component in components)
                    {
                        if (component is not IEntityComponent entityComponent) continue;
                        componentAddedPacket.ComponentType = entityComponent.ComponentType;
                        _server.SendPacket(entityNetworkComponent.NetPeer, componentAddedPacket);
                    }
                });
            }
            else if (@event.Component.Entity.TryGet<NetworkComponent>(out var netComponent) && netComponent is { NetPeer: not null })
            {
                var componentAddedPacket = new AddComponentPacket() { NetworkId = netComponent.NetworkId };
                //Loop through all entities and notify of the changes.
                World.Query(in query, (ref NetworkComponent entityNetworkComponent) =>
                {
                    if (entityNetworkComponent.NetPeer == null)
                        return; //not a client. (we also want to notify the local client because of server sided changes)
                    _server.SendPacket(entityNetworkComponent.NetPeer, componentAddedPacket);
                });
            }
        }

        private void OnComponentRemoved(ComponentRemovedEvent @event)
        {
            if (@event.Component is not NetworkComponent networkComponent) return;
            var query = new QueryDescription()
                .WithAll<NetworkComponent>();

            //Disconnect local client. This should only happen when the server destroys the entity.
            networkComponent.NetPeer?.Disconnect();

            var entityDestroyedPacket = new EntityDestroyedPacket() { NetworkId = networkComponent.NetworkId };
            World.Query(in query, (ref NetworkComponent netComponent) =>
            {
                if (netComponent.NetPeer == null) return; //not a client.
                _server.SendPacket(netComponent.NetPeer, entityDestroyedPacket);
            });
        }
    }
}