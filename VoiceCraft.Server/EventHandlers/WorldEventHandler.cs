using Arch.Core;
using Arch.Core.Extensions;
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
            var query = new QueryDescription()
                .WithAll<NetworkComponent>();
            
            var packet = new EntityDestroyedPacket() { Id = networkComponent.NetworkId };
            _world.Query(in query, entity =>
            {
                var netComponent = entity.Get<NetworkComponent>();
                _server.SendPacket(netComponent.Peer, packet);
            });
        }
        
        private void OnComponentAdded(Entity entity, object component)
        {
            throw new NotImplementedException();
        }
        
        private void OnComponentRemoved(Entity entity, Type componenttype)
        {
            throw new NotImplementedException();
        }
    }
}