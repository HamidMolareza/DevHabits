using System.Collections.Concurrent;
using System.Reflection;

namespace DevHabits.Api.Shared.Libraries.DataShaping;

/// <summary>
/// Provides a cache for mapping property names to PropertyInfo for types.
/// </summary>
internal static class PropertyMapCache {
    private static readonly ConcurrentDictionary<Type, Dictionary<string, PropertyInfo>> PropsCache = new();

    /// <summary>
    /// Gets a dictionary mapping property names to PropertyInfo for the given type.
    /// </summary>
    public static Dictionary<string, PropertyInfo> GetPropertiesMap(this Type type) {
        return PropsCache.GetOrAdd(type, t => {
            var dict = t.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .ToDictionary(p => p.Name, p => p, StringComparer.OrdinalIgnoreCase);
            return dict;
        });
    }
}
