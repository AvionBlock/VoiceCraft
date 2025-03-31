using VoiceCraft.Core;
using VoiceCraft.Core.Network.Packets;
using VoiceCraft.Server.Application;

namespace VoiceCraft.Server.Systems
{
    public class WorldSystem : IDisposable
    {
        private readonly VoiceCraftWorld _world;
        private readonly NetworkSystem _networkSystem;

        public WorldSystem(VoiceCraftServer server)
        {
            _world = server.World;
            _networkSystem = server.NetworkSystem;

            _world.OnEntityCreated += OnEntityCreated;
            _world.OnEntityDestroyed += OnEntityDestroyed;
        }
        
        public void Dispose()
        {
            _world.OnEntityCreated -= OnEntityCreated;
            _world.OnEntityDestroyed -= OnEntityDestroyed;
            GC.SuppressFinalize(this);
        }

        private void OnEntityCreated(VoiceCraftEntity newEntity)
        {
            //Visibility system will handle sending entity data.
            var createEntityPacket = new EntityCreatedPacket(newEntity.Id, newEntity);
            foreach (var entity in _world.Entities)
            {
                if (entity.Key == newEntity.Id || entity.Value is not VoiceCraftNetworkEntity networkEntity) continue;
                _networkSystem.SendPacket(networkEntity.NetPeer, createEntityPacket);
            }

            if (newEntity is not VoiceCraftNetworkEntity newNetworkEntity) return;
            foreach (var entity in _world.Entities.Where(entity => entity.Key != newNetworkEntity.Id))
            {
                createEntityPacket = new EntityCreatedPacket(entity.Key, entity.Value);
                _networkSystem.SendPacket(newNetworkEntity.NetPeer, createEntityPacket);
            }
        }

        private void OnEntityDestroyed(VoiceCraftEntity removedEntity)
        {
            var destroyEntityPacket = new EntityDestroyedPacket(removedEntity.Id);
            if (removedEntity is VoiceCraftNetworkEntity removedNetworkentity)
            {
                removedNetworkentity.NetPeer.Disconnect(); //Disconnect if it's a client entity.
            }

            foreach (var entity in _world.Entities)
            {
                if (entity.Value is not VoiceCraftNetworkEntity networkentity) continue;
                _networkSystem.SendPacket(networkentity.NetPeer, destroyEntityPacket);
            }
        }
    }
}