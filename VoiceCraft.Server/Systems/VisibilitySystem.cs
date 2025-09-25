using VoiceCraft.Core.World;
using VoiceCraft.Core.Interfaces;

namespace VoiceCraft.Server.Systems;

public class VisibilitySystem(VoiceCraftWorld world, AudioEffectSystem audioEffectSystem)
{
    private readonly Lock _lock = new();
    
    public void Update()
    {
        //Possible Race Condition. Will need to test further.
        Parallel.ForEach(world.Entities, UpdateVisibleNetworkEntities);
    }

    private void UpdateVisibleNetworkEntities(VoiceCraftEntity entity)
    {
        //Remove dead network entities.
        entity.TrimVisibleDeadEntities();

        //Add any new possible entities.
        var visibleNetworkEntities = world.Entities.OfType<VoiceCraftNetworkEntity>();
        foreach (var possibleEntity in visibleNetworkEntities)
        {
            try
            {
                if (possibleEntity.Id == entity.Id) continue;
                if (!EntityVisibility(entity, possibleEntity))
                {
                    _lock.Enter();
                    entity.RemoveVisibleEntity(possibleEntity);
                    continue;
                }
                
                _lock.Enter();
                entity.AddVisibleEntity(possibleEntity);
            }
            finally
            {
                if(_lock.IsHeldByCurrentThread)
                    _lock.Exit();
            }
        }
    }

    private bool EntityVisibility(VoiceCraftEntity from, VoiceCraftNetworkEntity to)
    {
        if ((from.TalkBitmask & to.ListenBitmask) == 0) return false;
        foreach (var effect in audioEffectSystem.Effects)
        {
            if (effect.Value is not IVisible visibleEffect) continue;
            if (!visibleEffect.Visibility(from, to, effect.Key)) return false;
        }

        return true;
    }
}