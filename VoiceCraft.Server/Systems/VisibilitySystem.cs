using VoiceCraft.Core;
using VoiceCraft.Core.Network.Packets;
using VoiceCraft.Server.Application;

namespace VoiceCraft.Server.Systems
{
    public class VisibilitySystem(VoiceCraftServer server)
    {
        private readonly VoiceCraftWorld _world = server.World;
        private readonly NetworkSystem _networkSystem = server.NetworkSystem;
        private readonly List<Task> _tasks = [];

        public void Update()
        {
            foreach (var entity in _world.Entities)
            {
                _tasks.Add(Task.Run(() =>
                {
                    UpdateVisibleNetworkEntities(entity.Value);
                }));
            }
            
            var task = Task.WhenAll(_tasks);
            task.Wait();
            _tasks.Clear();
        }

        private void UpdateVisibleNetworkEntities(VoiceCraftEntity entity)
        {
            //Remove dead network entities.
            entity.VisibleEntities.RemoveAll(x => x.Destroyed);
            
            //Add any new possible entities.
            foreach (var possibleEntity in _world.Entities)
            {
                if(possibleEntity.Key == entity.Id || possibleEntity.Value is not VoiceCraftNetworkEntity possibleNetworkEntity) continue;
                if (!entity.VisibleTo(possibleNetworkEntity))
                {
                    entity.VisibleEntities.Remove(possibleNetworkEntity);
                    SendEntityDestroyed(entity, possibleNetworkEntity);
                    continue;
                }
                
                if(!entity.VisibleEntities.Contains(possibleNetworkEntity)) continue;
                entity.VisibleEntities.Add(possibleNetworkEntity);
                SendEntityCreated(entity, possibleNetworkEntity);
            }
        }

        private void SendEntityCreated(VoiceCraftEntity entity, VoiceCraftNetworkEntity targetEntity)
        {
            var entityCreatedPacket = new EntityCreatedPacket(entity.Id, entity);
            _networkSystem.SendPacket(targetEntity.NetPeer, entityCreatedPacket);
        }

        private void SendEntityDestroyed(VoiceCraftEntity entity, VoiceCraftNetworkEntity targetEntity)
        {
            var entityDestroyedPacket = new EntityDestroyedPacket(entity.Id);
            _networkSystem.SendPacket(targetEntity.NetPeer, entityDestroyedPacket);
        }
    }
}