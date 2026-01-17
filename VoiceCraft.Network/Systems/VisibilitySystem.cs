using System.Linq;
using System.Threading.Tasks;
using VoiceCraft.Core.Interfaces;
using VoiceCraft.Core.World;
using VoiceCraft.Network.World;

namespace VoiceCraft.Network.Systems;

public class VisibilitySystem(VoiceCraftWorld world, AudioEffectSystem audioEffectSystem)
{
    public void Update()
    {
        Parallel.ForEach(world.Entities, UpdateVisibleNetworkEntities);
    }

    private void UpdateVisibleNetworkEntities(VoiceCraftEntity entity)
    {
        //Remove dead network entities.
        entity.TrimDeadEntities();

        //Add any new possible entities.
        var visibleNetworkEntities = world.Entities.OfType<VoiceCraftNetworkEntity>();
        foreach (var possibleEntity in visibleNetworkEntities)
        {
            if (possibleEntity.Id == entity.Id) continue;
            if (!EntityVisibility(entity, possibleEntity))
            {
                entity.RemoveVisibleEntity(possibleEntity);
                continue;
            }

            entity.AddVisibleEntity(possibleEntity);
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