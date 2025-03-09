using Arch.Core;
using Arch.Core.Extensions;
using VoiceCraft.Core;
using VoiceCraft.Core.Components;
using VoiceCraft.Core.Network.Packets;

namespace VoiceCraft.Server.EventHandlers
{
    public class WorldEventHandler
    {
        private readonly VoiceCraftServer _server;
        private readonly WorldHandler _world;
        
        public WorldEventHandler(VoiceCraftServer server)
        {
            _server = server;
           _world = _server.World; 
           
           _world.OnEntityCreated += OnEntityCreated;
           _world.OnEntityDestroyed += OnEntityDestroyed;
           _world.OnComponentAdded += OnComponentAdded;
           _world.OnComponentRemoved += OnComponentRemoved;
        }

        private void OnEntityCreated(Entity entity)
        {
            throw new NotImplementedException();
        }

        private void OnEntityDestroyed(Entity destroyedEntity)
        {
            if (!_world.TryGet<NetworkComponent>(destroyedEntity, out var networkComponent) || networkComponent == null) return;
            Broadcast(new EntityDestroyedPacket() { Id = networkComponent.NetworkId });
        }
        
        private void OnComponentAdded(Entity entity, object component)
        {
            //Get visible entities then do shit.
            throw new NotImplementedException();
        }
        
        private void OnComponentRemoved(Entity entity, object component)
        {
            if (component is IComponentSerializable serializableComponent && _world.Has<NetworkComponent>(entity))
            {
                var netComponent = _world.Get<NetworkComponent>(entity);
                Broadcast(new RemoveComponentPacket() { ComponentType = serializableComponent.GetType().Name, NetworkId = netComponent.NetworkId });
            }
                
            if (component is not NetworkComponent networkComponent) return;
            Broadcast(new EntityDestroyedPacket() { Id = networkComponent.NetworkId });
        } 
        
        private void Broadcast(VoiceCraftPacket packet)
        {
            var query = new QueryDescription()
                .WithAll<NetworkComponent>();
            
            _world.Query(in query, entity =>
            {
                var networkComponent = entity.Get<NetworkComponent>();
                _server.SendPacket(networkComponent.Peer, packet);
            });
        }
    }
}