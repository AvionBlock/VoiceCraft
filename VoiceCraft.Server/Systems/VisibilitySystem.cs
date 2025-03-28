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
            RemoveDeadNetworkEntities(entity);
            entity.VisibleEntities.TryAdd(entity.Id, entity); //Should always be visible to itself.
            foreach (var possibleEntity in _world.Entities)
            {
                if(possibleEntity.Value is not VoiceCraftNetworkEntity visibleNetworkEntity) continue;
                if (!entity.VisibleTo(visibleNetworkEntity))
                {
                    entity.VisibleEntities.TryRemove(possibleEntity.Key, out _);
                    if(possibleEntity.Value is VoiceCraftNetworkEntity possibleNetworkEntity)
                        SendEntityDestroyed(entity, possibleNetworkEntity);
                    continue;
                }
                
                if(!entity.VisibleEntities.TryAdd(possibleEntity.Key, visibleNetworkEntity)) continue;
                SendEntityCreated(entity, visibleNetworkEntity);
            }
        }

        private void RemoveDeadNetworkEntities(VoiceCraftEntity entity)
        {
            foreach (var visibleEntity in _world.Entities)
            {
                if(!visibleEntity.Value.Destroyed) continue;
                entity.VisibleEntities.Remove(visibleEntity.Key, out _);
            }
        }

        private void SendEntityCreated(VoiceCraftEntity entity, VoiceCraftNetworkEntity targetEntity)
        {
            var entityCreatedPacket = new EntityCreatedPacket(entity);
            _networkSystem.SendPacket(targetEntity.NetPeer, entityCreatedPacket);

            var addEffectPacket = new SetEffectPacket(entity.Id, null);
            foreach (var effect in entity.Effects)
            {
                addEffectPacket.Effect = effect.Value;
                _networkSystem.SendPacket(targetEntity.NetPeer, addEffectPacket);
            }
        }

        private void SendEntityDestroyed(VoiceCraftEntity entity, VoiceCraftNetworkEntity targetEntity)
        {
            var entityDestroyedPacket = new EntityDestroyedPacket(entity.Id);
            _networkSystem.SendPacket(targetEntity.NetPeer, entityDestroyedPacket);
        }
    }
}