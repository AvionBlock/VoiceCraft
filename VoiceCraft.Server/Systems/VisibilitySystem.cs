using VoiceCraft.Core;
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
                    continue;
                }
                
                if(!entity.VisibleEntities.TryAdd(possibleEntity.Key, visibleNetworkEntity)) continue;
                _networkSystem.SendEntityData(entity, visibleNetworkEntity);
                _networkSystem.SendEntityEffectUpdates(entity, visibleNetworkEntity);
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
    }
}