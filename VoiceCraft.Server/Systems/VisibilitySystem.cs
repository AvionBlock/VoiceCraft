using VoiceCraft.Core;

namespace VoiceCraft.Server.Systems
{
    public class VisibilitySystem(VoiceCraftServer server)
    {
        private readonly VoiceCraftWorld _world = server.World;
        private readonly NetworkSystem _networkSystem = server.NetworkSystem;

        public void Update()
        {
            foreach (var entity in _world.Entities)
            {
                UpdateVisibleNetworkEntities(entity.Value);
            }
        }

        private void UpdateVisibleNetworkEntities(VoiceCraftEntity entity)
        {
            entity.VisibleEntities.RemoveAll(x => x.Destroyed);
            foreach (var visibleEntity in _world.Entities)
            {
                if(visibleEntity.Value == entity || visibleEntity.Value is not VoiceCraftNetworkEntity visibleNetworkEntity) continue;
                if (!entity.VisibleTo(visibleNetworkEntity))
                {
                    entity.VisibleEntities.Remove(visibleNetworkEntity);
                    continue;
                }

                if (entity.VisibleEntities.Contains(visibleNetworkEntity)) continue;
                entity.VisibleEntities.Add(visibleNetworkEntity);
                _networkSystem.SendEntityData(entity, visibleNetworkEntity);
                _networkSystem.SendEntityEffects(entity, visibleNetworkEntity);
            }
        }
    }
}