using System.Reflection;

namespace DevHabits.Api.Shared.Libraries.DataShaping;

/// <summary>
/// Validates requested fields against DTO properties.
/// </summary>
internal static class FieldValidator {
    /// <summary>
    /// Validates top-level field tokens against DTO properties.
    /// </summary>
    /// <param name="tokens">Field tokens.</param>
    /// <param name="dtoProps">DTO property map.</param>
    /// <param name="dtoType">DTO type.</param>
    /// <param name="error">Error message if validation fails.</param>
    /// <returns>Grouped dictionary of top-level fields and nested fields, or null if invalid.</returns>
    /// <example>
    /// <code>
    /// var grouped = FieldValidator.ValidateFields(
    ///     new[] { "Name", "Profile.Email" }, FieldSelector.GetPropertiesMap(typeof(UserDto)), typeof(UserDto), out var error);
    /// // grouped["Name"] == null, grouped["Profile"] == ["Email"]
    /// </code>
    /// </example>
    public static Dictionary<string, List<string>>? ValidateFields(
        string[] tokens,
        Dictionary<string, PropertyInfo> dtoProps,
        Type dtoType,
        out string? error) {
        var grouped = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        tokens = tokens.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();

        foreach (string token in tokens) {
            string[] parts = token.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length == 0)
                continue;

            string top = parts[0];
            if (!dtoProps.TryGetValue(top, out PropertyInfo? topProp)) {
                error = $"Field '{token}' is not a top-level property of {dtoType.Name}.";
                return null;
            }

            if (parts.Length == 1) {
                grouped[top] = null!;
                continue;
            }

            if (parts.Length == 2) {
                Type nestedType = topProp.PropertyType;
                if (nestedType.IsGenericType && nestedType.GetGenericTypeDefinition() == typeof(Nullable<>)) {
                    nestedType = Nullable.GetUnderlyingType(nestedType)!;
                }

                Dictionary<string, PropertyInfo> nestedProps = nestedType.GetPropertiesMap();
                string nested = parts[1];
                if (!nestedProps.ContainsKey(nested)) {
                    error = $"Field '{token}' is not a property of '{nestedType.Name}'.";
                    return null;
                }

                if (grouped.TryGetValue(top, out List<string>? list)) {
                    if (list == null!) {
                        continue;
                    }
                }
                else {
                    list = [];
                    grouped[top] = list;
                }

                list.Add(nested);
                continue;
            }

            error = $"Field '{token}' depth > 2 is not supported.";
            return null;
        }

        error = null;
        return grouped;
    }

    // Overload for config-based validation
    public static Dictionary<string, List<string>?>? ValidateFields<TEntity, TDto>(
        string[] tokens,
        DtoMappingConfiguration<TEntity, TDto> config,
        out string? error) where TDto : class {
        var grouped = new Dictionary<string, List<string>?>(StringComparer.OrdinalIgnoreCase);
        tokens = tokens.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();

        foreach (string token in tokens) {
            string[] parts = token.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length == 0)
                continue;

            string top = parts[0];

            // === Top-level ===
            if (parts.Length == 1) {
                if (!config.Mappings.ContainsKey(top) &&
                    !config.NestedGroups.ContainsKey(top) &&
                    !config.CollectionMappings.ContainsKey(top)) {
                    error = $"Field '{token}' is not mapped.";
                    return null;
                }

                grouped[top] = null!;
                continue;
            }

            // === Two-level ===
            if (parts.Length == 2) {
                string nested = parts[1];
                string path = $"{top}.{nested}";

                // check primitive/complex mapping
                bool isValid =
                    config.Mappings.ContainsKey(path) ||
                    config.CollectionMappings.TryGetValue(top, out CollectionMapping? collection)
                && ((dynamic)collection.NestedConfig).Mappings.ContainsKey(path);

                if (!isValid) {
                    error = $"Field '{token}' is not mapped.";
                    return null;
                }

                if (grouped.TryGetValue(top, out List<string>? list)) {
                    // already marked as "full include"
                    if (list == null!)
                        continue;
                }
                else {
                    list = [];
                    grouped[top] = list;
                }

                if (!list.Contains(nested, StringComparer.OrdinalIgnoreCase))
                    list.Add(nested);

                continue;
            }

            // === More than two levels not supported ===
            error = $"Field '{token}' depth > 2 is not supported.";
            return null;
        }

        error = null;
        return grouped;
    }
}
