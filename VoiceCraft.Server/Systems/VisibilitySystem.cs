using VoiceCraft.Core;
using VoiceCraft.Server.Application;

namespace VoiceCraft.Server.Systems
{
    public class VisibilitySystem(VoiceCraftServer server)
    {
        private readonly VoiceCraftWorld _world = server.World;
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
            entity.TrimVisibleDeadEntities();
            
            //Add any new possible entities.
            foreach (var possibleEntity in _world.Entities)
            {
                if(possibleEntity.Key == entity.Id || possibleEntity.Value is not VoiceCraftNetworkEntity possibleNetworkEntity) continue;
                if (!entity.VisibleTo(possibleNetworkEntity))
                {
                    entity.RemoveVisibleEntity(possibleNetworkEntity);
                    continue;
                }
                
                entity.AddVisibleEntity(possibleNetworkEntity);
            }
        }
    }
}