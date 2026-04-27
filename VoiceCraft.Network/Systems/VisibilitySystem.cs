using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using VoiceCraft.Core.Interfaces;
using VoiceCraft.Core.World;
using VoiceCraft.Network.Interfaces;
using VoiceCraft.Network.World;

namespace VoiceCraft.Network.Systems;

public class VisibilitySystem(VoiceCraftWorld world, AudioEffectSystem audioEffectSystem)
{
    public void Update()
    {
        var entities = world.Entities.ToArray();
        var visibleNetworkEntities = entities.OfType<VoiceCraftNetworkEntity>().ToArray();
        var audioEffects = audioEffectSystem.AudioEffects;
        if(audioEffects == null) return;

        Parallel.ForEach(entities,
            entity => UpdateVisibleNetworkEntities(entity, visibleNetworkEntities, audioEffects));
    }

    private static void UpdateVisibleNetworkEntities(
        VoiceCraftEntity entity,
        VoiceCraftNetworkEntity[] visibleNetworkEntities,
        IImmutableDictionary<ushort, IAudioEffect> audioEffects)
    {
        //Remove dead network entities.
        entity.TrimDeadEntities();

        //Add any new possible entities.
        foreach (var possibleEntity in visibleNetworkEntities)
        {
            if (possibleEntity.Id == entity.Id) continue;
            if (!EntityVisibility(entity, possibleEntity, audioEffects))
            {
                entity.RemoveVisibleEntity(possibleEntity);
                continue;
            }

            entity.AddVisibleEntity(possibleEntity);
        }
    }

    private static bool EntityVisibility(
        VoiceCraftEntity from,
        VoiceCraftNetworkEntity to,
        IImmutableDictionary<ushort, IAudioEffect> audioEffects)
    {
        if ((from.TalkBitmask & to.ListenBitmask) == 0) return false;
        foreach (var effect in audioEffects)
        {
            if (effect.Value is not IVisible visibleEffect) continue;
            if (!visibleEffect.Visibility(from, to, effect.Key)) return false;
        }

        return true;
    }
}
