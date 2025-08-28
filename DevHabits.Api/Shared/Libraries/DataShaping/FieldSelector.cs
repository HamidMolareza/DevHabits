using System.Reflection;

namespace DevHabits.Api.Shared.Libraries.DataShaping;

/// <summary>
/// Provides utilities for selecting fields from DTOs dynamically based on a field string.
/// </summary>
public static class FieldSelector {
    /// <summary>
    /// Attempts to create a selector function for the specified DTO type based on requested fields.
    /// </summary>
    /// <typeparam name="TDto">The DTO type.</typeparam>
    /// <param name="fields">Comma-separated list of fields to select.</param>
    /// <param name="selector">Output selector function.</param>
    /// <param name="error">Error message if creation fails.</param>
    /// <returns>True if selector was created successfully; otherwise, false.</returns>
    /// <example>
    /// <code>
    /// // Given a DTO:
    /// public class UserDto { public string Name { get; set; } public int Age { get; set; } }
    /// // Usage:
    /// bool ok = FieldSelector.TryCreateSelector&lt;UserDto&gt;("Name", out var selector, out var error);
    /// var shaped = selector(new UserDto { Name = "Alice", Age = 30 }); // returns { "Name": "Alice" }
    /// </code>
    /// </example>
    public static bool TryCreateSelector<TDto>(string? fields,
        out Func<TDto, object> selector,
        out string? error) where TDto : class {
        selector = _ => throw new InvalidOperationException("uninitialized");
        error = null;

        // If no fields requested, return the DTO itself
        if (string.IsNullOrWhiteSpace(fields)) {
            selector = dto => dto;
            return true;
        }

        string[] tokens = TokenParser.Parse(fields);
        if (tokens.Length == 0) {
            selector = dto => dto;
            return true;
        }

        Type dtoType = typeof(TDto);
        Dictionary<string, PropertyInfo> dtoProps = dtoType.GetPropertiesMap();

        // Validate fields and group nested properties
        Dictionary<string, List<string>>? grouped =
            FieldValidator.ValidateFields(tokens, dtoProps, dtoType, out error);
        if (grouped == null)
            return false;

        // Extract ordered unique top-level keys from tokens
        var orderedTops = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (string token in tokens) {
            string[] parts = token.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length == 0)
                continue;
            string top = parts[0];
            if (seen.Add(top)) {
                orderedTops.Add(top);
            }
        }

        // Compute included grouped with null for whole
        var includedGrouped = new Dictionary<string, List<string>?>(StringComparer.OrdinalIgnoreCase);
        foreach (KeyValuePair<string, List<string>> kv in grouped) {
            includedGrouped[kv.Key] = kv.Value;
        }

        // Compile accessors for top-level and nested properties
        List<(string requestedKey, Func<TDto, object> getter)> topAccessors =
            AccessorCompiler.BuildAccessors<TDto>(includedGrouped, dtoProps);

        // Create the selector function
        selector = SelectorFactory.Create(orderedTops, topAccessors);

        return true;
    }

    /// <summary>
    /// Attempts to create an excluder function for the specified DTO type based on excluded fields.
    /// </summary>
    /// <typeparam name="TDto">The DTO type.</typeparam>
    /// <param name="excludeFields">Comma-separated list of fields to exclude.</param>
    /// <param name="excluder">Output excluder function.</param>
    /// <param name="error">Error message if creation fails.</param>
    /// <returns>True if excluder was created successfully; otherwise, false.</returns>
    /// <example>
    /// <code>
    /// // Given a DTO:
    /// public class UserDto { public string Name { get; set; } public int Age { get; set; } }
    /// // Usage:
    /// bool ok = FieldSelector.TryCreateExcluder&lt;UserDto&gt;("Age", out var excluder, out var error);
    /// var shaped = excluder(new UserDto { Name = "Alice", Age = 30 }); // returns { "Name": "Alice" }
    /// </code>
    /// </example>
    public static bool TryCreateExcluder<TDto>(string? excludeFields,
        out Func<TDto, object> excluder,
        out string? error) where TDto : class {
        excluder = _ => throw new InvalidOperationException("uninitialized");
        error = null;

        // If no fields excluded, return the DTO itself
        if (string.IsNullOrWhiteSpace(excludeFields)) {
            excluder = dto => dto;
            return true;
        }

        string[] tokens = TokenParser.Parse(excludeFields);
        if (tokens.Length == 0) {
            excluder = dto => dto;
            return true;
        }

        Type dtoType = typeof(TDto);
        Dictionary<string, PropertyInfo> dtoProps = dtoType.GetPropertiesMap();

        // Validate excluded fields and group nested properties
        Dictionary<string, List<string>>? excludedGrouped =
            FieldValidator.ValidateFields(tokens, dtoProps, dtoType, out error);
        if (excludedGrouped == null)
            return false;

        // Get all top-level properties in declaration order
        PropertyInfo[] allProps = dtoType.GetProperties(BindingFlags.Instance | BindingFlags.Public);
        var allTops = allProps.Select(p => p.Name).ToList();

        // Compute fully excluded tops
        var fullyExcluded = new HashSet<string>(
            excludedGrouped.Where(kv => kv.Value == null).Select(kv => kv.Key),
            StringComparer.OrdinalIgnoreCase);

        // Compute ordered tops for inclusion (in property declaration order)
        var orderedTops = new List<string>();
        foreach (string top in allTops) {
            if (!fullyExcluded.Contains(top)) {
                orderedTops.Add(top);
            }
        }

        // Compute included grouped
        var includedGrouped = new Dictionary<string, List<string>?>(StringComparer.OrdinalIgnoreCase);
        foreach (string top in orderedTops) {
            if (!excludedGrouped.TryGetValue(top, out List<string>? exclNested) || exclNested == null) {
                // Whole property included
                includedGrouped[top] = null;
                continue;
            }

            // Partially excluded: compute included nested fields
            Type nestedType = dtoProps[top].PropertyType;
            if (nestedType.IsGenericType && nestedType.GetGenericTypeDefinition() == typeof(Nullable<>)) {
                nestedType = Nullable.GetUnderlyingType(nestedType)!;
            }

            Dictionary<string, PropertyInfo> nestedProps = nestedType.GetPropertiesMap();
            var exclSet = new HashSet<string>(exclNested, StringComparer.OrdinalIgnoreCase);
            var inclNested = nestedProps.Keys.Except(exclSet, StringComparer.OrdinalIgnoreCase).ToList();

            includedGrouped[top] = inclNested;
        }

        // Compile accessors for top-level and nested properties
        List<(string requestedKey, Func<TDto, object> getter)> topAccessors =
            AccessorCompiler.BuildAccessors<TDto>(includedGrouped, dtoProps);

        // Create the excluder function
        excluder = SelectorFactory.Create(orderedTops, topAccessors);

        return true;
    }
}
