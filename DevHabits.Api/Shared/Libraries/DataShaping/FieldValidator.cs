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
    /// var grouped = FieldValidator.ValidateTopLevelFields(
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
        tokens = tokens.Distinct().ToArray();

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
                // whole property requested
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
                    error =
                        $"field '{token}' is not a property of '{nestedType.Name}'.";
                    return null;
                }

                if (grouped.TryGetValue(top, out List<string>? list)) {
                    if (list == null!) {
                        // already requested the whole object; ignore subfields
                        continue;
                    }
                    // else: Another path like "top.subfield" was already requested
                }
                else {
                    // first time seeing this top-level property
                    list = [];
                    grouped[top] = list;
                }

                list.Add(nested);
                continue;
            }

            // ❌ depth > 2
            error = $"field '{token}' depth > 2 is not supported.";
            return null;
        }

        error = null;
        return grouped;
    }
}
