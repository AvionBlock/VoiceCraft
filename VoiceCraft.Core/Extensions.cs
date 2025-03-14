using Arch.Core;
using Arch.Core.Extensions;
using VoiceCraft.Core.Components;

namespace VoiceCraft.Core
{
    public static class Extensions
    {
        public static string? Truncate(this string? value, int maxLength, string truncationSuffix = "…")
        {
            return value?.Length > maxLength
                ? value[..maxLength] + truncationSuffix
                : value;
        }

        public static Entity GetEntityFromNetworkId(this World world, int networkId)
        {
            var entity = Entity.Null;
            world.Query(in NetworkComponent.Query, (ref NetworkComponent component) =>
            {
                if (entity != Entity.Null || component.NetworkId != networkId) return;
                entity = component.Entity;
            });
            
            return entity;
        }

        public static object? GetComponentFromReference<T>(this World world, ComponentReference componentReference) where T : notnull
        {
            var entity = world.GetEntityFromNetworkId(componentReference.NetworkId);
            if (entity == Entity.Null) return null;
            
            var components = entity.GetAllComponents();
            foreach (var component in components)
            {
                if (component is ISerializableEntityComponent entityComponent &&
                    entityComponent.ComponentType == componentReference.ComponentType &&
                    entityComponent is T componentType)
                {
                    return componentType;
                }
            }
            
            return null;
        }
    }
}