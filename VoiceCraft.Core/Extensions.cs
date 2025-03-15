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

        public static object? GetComponentFromReference<T>(this World world, ComponentReference componentReference) where T : notnull
        {
            var networkComponent = NetworkComponent.GetNetworkComponentFromId(componentReference.NetworkId);
            if (networkComponent == null) return null;
            
            var components = networkComponent.Entity.GetAllComponents();
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