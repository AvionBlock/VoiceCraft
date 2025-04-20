using VoiceCraft.Core;
using VoiceCraft.Server.Application;
using VoiceCraft.Server.Data;

namespace VoiceCraft.Server.Systems
{
    public class VisibilitySystem(VoiceCraftServer server)
    {
        private readonly VoiceCraftWorld _world = server.World;

        public void Update()
        {
            Parallel.ForEach(_world.Entities, UpdateVisibleNetworkEntities);
        }

        private void UpdateVisibleNetworkEntities(VoiceCraftEntity entity)
        {
            //Remove dead network entities.
            entity.TrimVisibleDeadEntities();
            
            //Add any new possible entities.
            var visibleNetworkEntities = _world.Entities.OfType<VoiceCraftNetworkEntity>();
            foreach (var possibleEntity in visibleNetworkEntities)
            {
                if(possibleEntity.Id == entity.Id) continue;
                if (!entity.VisibleTo(possibleEntity))
                {
                    entity.RemoveVisibleEntity(possibleEntity);
                    continue;
                }
                
                entity.AddVisibleEntity(possibleEntity);
            }
        }
    }
}