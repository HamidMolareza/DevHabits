using System.Linq.Expressions;
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
        if (grouped == null!)
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
        if (excludedGrouped == null!)
            return false;

        // Get all top-level properties in declaration order
        PropertyInfo[] allProps = dtoType.GetProperties(BindingFlags.Instance | BindingFlags.Public);
        var allTops = allProps.Select(p => p.Name).ToList();

        // Compute fully excluded tops
        var fullyExcluded = new HashSet<string>(
            excludedGrouped.Where(kv => kv.Value == null!).Select(kv => kv.Key),
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
            if (!excludedGrouped.TryGetValue(top, out List<string>? exclNested) || exclNested == null!) {
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

    /// <summary>
    /// Attempts to create a shaper function that includes specified fields while excluding others.
    /// </summary>
    /// <typeparam name="TDto">The DTO type.</typeparam>
    /// <param name="includeFields">Comma-separated fields to include.</param>
    /// <param name="excludeFields">Comma-separated fields to exclude.</param>
    /// <param name="shaper">Output shaper function.</param>
    /// <param name="error">Error message if creation fails.</param>
    /// <returns>True if shaper was created successfully; otherwise, false.</returns>
    /// <example>
    /// <code>
    /// // Given a DTO:
    /// public class UserDto { public string Name { get; set; } public ProfileDto Profile { get; set; } }
    /// public class ProfileDto { public string Email { get; set; } public string Phone { get; set; } }
    /// // Usage:
    /// bool ok = FieldSelector.TryCreateShaper&lt;UserDto&gt;("Name,Profile.Email", "Profile.Phone", out var shaper, out var error);
    /// var shaped = shaper(new UserDto { Name = "Alice", Profile = new ProfileDto { Email = "a@b.com", Phone = "123" } });
    /// // returns { "Name": "Alice", "Profile": { "Email": "a@b.com" } }
    /// </code>
    /// </example>
    public static bool TryCreateShaper<TDto>(string includeFields,
        string excludeFields,
        out Func<TDto, object> shaper,
        out string? error) where TDto : class {
        shaper = _ => throw new InvalidOperationException("uninitialized");
        error = null;

        string[] includeTokens = TokenParser.Parse(includeFields);
        string[] excludeTokens = TokenParser.Parse(excludeFields);

        if (includeTokens.Length == 0 && excludeTokens.Length == 0) {
            shaper = dto => dto;
            return true;
        }

        if (includeTokens.Length == 0)
            return TryCreateExcluder(excludeFields, out shaper, out error);

        if (excludeTokens.Length == 0)
            return TryCreateSelector(includeFields, out shaper, out error);

        var common = includeTokens.Intersect(excludeTokens, StringComparer.OrdinalIgnoreCase).ToList();
        if (common.Count > 0) {
            error = $"Fields cannot be both included and excluded: {string.Join(", ", common)}";
            shaper = null!;
            return false;
        }

        Type dtoType = typeof(TDto);
        Dictionary<string, PropertyInfo> dtoProps = dtoType.GetPropertiesMap();

        // Validate include fields
        Dictionary<string, List<string>>? includedGrouped =
            FieldValidator.ValidateFields(includeTokens, dtoProps, dtoType, out error);
        if (includedGrouped == null!) {
            shaper = null!;
            return false;
        }

        // Validate exclude fields
        Dictionary<string, List<string>>? excludedGrouped =
            FieldValidator.ValidateFields(excludeTokens, dtoProps, dtoType, out error);
        if (excludedGrouped == null!) {
            shaper = null!;
            return false;
        }

        // Compute effective included fields: include fields minus exclude fields
        var effectiveGrouped = new Dictionary<string, List<string>?>(StringComparer.OrdinalIgnoreCase);
        var orderedTops = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (string token in includeTokens) {
            string[] parts = token.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length == 0)
                continue;
            string top = parts[0];
            if (!seen.Add(top))
                continue;
            orderedTops.Add(top);

            if (!includedGrouped.ContainsKey(top))
                continue;

            if (excludedGrouped.TryGetValue(top, out List<string>? exclNested) && exclNested == null!) {
                // Top-level property fully excluded, skip it
                // continue;
                error = $"field {token} included but its top-level property '{top}' is fully excluded.";
                shaper = null!;
                return false;
            }

            if (includedGrouped[top] == null!) {
                // Whole top-level property included
                if (exclNested != null) {
                    // Exclude specific nested fields
                    Type nestedType = dtoProps[top].PropertyType;
                    if (nestedType.IsGenericType && nestedType.GetGenericTypeDefinition() == typeof(Nullable<>)) {
                        nestedType = Nullable.GetUnderlyingType(nestedType)!;
                    }

                    Dictionary<string, PropertyInfo> nestedProps = nestedType.GetPropertiesMap();
                    var exclSet = new HashSet<string>(exclNested, StringComparer.OrdinalIgnoreCase);
                    var inclNested =
                        nestedProps.Keys.Except(exclSet, StringComparer.OrdinalIgnoreCase).ToList();
                    effectiveGrouped[top] = inclNested.Count > 0 ? inclNested : null;
                }
                else {
                    effectiveGrouped[top] = null;
                }
            }
            else {
                // Specific nested fields included
                var inclNested = new HashSet<string>(includedGrouped[top], StringComparer.OrdinalIgnoreCase);
                if (exclNested != null) {
                    var exclSet = new HashSet<string>(exclNested, StringComparer.OrdinalIgnoreCase);
                    inclNested.ExceptWith(exclSet);
                }

                effectiveGrouped[top] = inclNested.Count > 0 ? [.. inclNested] : null;
            }
        }

        if (effectiveGrouped.Count == 0) {
            shaper = null!;
            error = "No fields remain after applying exclusions.";
            return false;
        }

        // Compile accessors for effective fields
        List<(string requestedKey, Func<TDto, object> getter)> topAccessors =
            AccessorCompiler.BuildAccessors<TDto>(effectiveGrouped, dtoProps);

        // Create the shaper function
        shaper = SelectorFactory.Create(orderedTops, topAccessors);
        return true;
    }

    public static bool TryCreateSelectorProjection<TEntity, TDto>(string? fields,
        DtoMappingConfiguration<TEntity, TDto> config,
        out Expression<Func<TEntity, object>> projection,
        out string? error) where TDto : class {
        projection = null!;

        if (string.IsNullOrWhiteSpace(fields))
            return TryCreateFullProjection(config, out projection, out error);

        string[] tokens = TokenParser.Parse(fields);
        if (tokens.Length == 0)
            return TryCreateFullProjection(config, out projection, out error);

        Dictionary<string, List<string>?> grouped =
            FieldValidator.ValidateFields(tokens, config, out error);
        if (grouped == null)
            return false;

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

        projection = ProjectionCompiler.BuildProjection(orderedTops, grouped, config);
        return true;
    }

    public static bool TryCreateExcludeProjection<TEntity, TDto>(string? excludeFields,
        DtoMappingConfiguration<TEntity, TDto> config,
        out Expression<Func<TEntity, object>> projection,
        out string? error) where TDto : class {
        projection = null!;
        error = null;

        if (string.IsNullOrWhiteSpace(excludeFields))
            return TryCreateFullProjection(config, out projection, out error);

        string[] tokens = TokenParser.Parse(excludeFields);
        if (tokens.Length == 0)
            return TryCreateFullProjection(config, out projection, out error);

        Dictionary<string, List<string>?>? excludedGrouped =
            FieldValidator.ValidateFields(tokens, config, out error);
        if (excludedGrouped == null)
            return false;

        Type dtoType = typeof(TDto);
        var allTops = config.GetAllTopLevelFields(dtoType).ToList();

        var fullyExcluded = new HashSet<string>(
            excludedGrouped.Where(kv => kv.Value == null).Select(kv => kv.Key),
            StringComparer.OrdinalIgnoreCase);

        var orderedTops = new List<string>();
        foreach (string top in allTops) {
            if (!fullyExcluded.Contains(top)) {
                orderedTops.Add(top);
            }
        }

        var includedGrouped = new Dictionary<string, List<string>?>(StringComparer.OrdinalIgnoreCase);
        foreach (string top in orderedTops) {
            if (!excludedGrouped.TryGetValue(top, out List<string>? exclNested) || exclNested == null) {
                includedGrouped[top] = null;
                continue;
            }

            if (config.NestedGroups.TryGetValue(top, out List<string>? allNested)) {
                var exclSet = new HashSet<string>(exclNested, StringComparer.OrdinalIgnoreCase);
                var inclNested = allNested.Except(exclSet, StringComparer.OrdinalIgnoreCase).ToList();
                includedGrouped[top] = inclNested;
            }
        }

        projection = ProjectionCompiler.BuildProjection(orderedTops, includedGrouped, config);
        return true;
    }

    public static bool TryCreateShapeProjection<TEntity, TDto>(string includeFields,
        string excludeFields,
        DtoMappingConfiguration<TEntity, TDto> config,
        out Expression<Func<TEntity, object>> projection,
        out string? error) where TDto : class {
        projection = null!;

        if (string.IsNullOrWhiteSpace(includeFields) && string.IsNullOrWhiteSpace(excludeFields))
            return TryCreateFullProjection(config, out projection, out error);

        string[] includeTokens = TokenParser.Parse(includeFields);
        string[] excludeTokens = TokenParser.Parse(excludeFields);

        if (includeTokens.Length == 0 && excludeTokens.Length == 0)
            return TryCreateFullProjection(config, out projection, out error);

        if (includeTokens.Length == 0)
            return TryCreateExcludeProjection(excludeFields, config, out projection, out error);

        if (excludeTokens.Length == 0)
            return TryCreateSelectorProjection(excludeFields, config, out projection, out error);

        var common = includeTokens.Intersect(excludeTokens, StringComparer.OrdinalIgnoreCase).ToList();
        if (common.Count > 0) {
            error = $"Fields cannot be both included and excluded: {string.Join(", ", common)}";
            projection = null!;
            return false;
        }

        Dictionary<string, List<string>?>? includedGrouped =
            FieldValidator.ValidateFields(includeTokens, config, out error);
        if (includedGrouped == null)
            return false;

        Dictionary<string, List<string>?>? excludedGrouped =
            FieldValidator.ValidateFields(excludeTokens, config, out error);
        if (excludedGrouped == null)
            return false;

        var effectiveGrouped = new Dictionary<string, List<string>?>(StringComparer.OrdinalIgnoreCase);
        var orderedTops = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (string token in includeTokens) {
            string[] parts = token.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length == 0)
                continue;
            string top = parts[0];
            if (!seen.Add(top))
                continue;
            orderedTops.Add(top);

            if (!includedGrouped.ContainsKey(top))
                continue;

            if (excludedGrouped.TryGetValue(top, out List<string>? exclNested) && exclNested == null) {
                continue;
            }

            if (includedGrouped[top] == null) {
                List<string>? inclNestedList = null;
                if (config.NestedGroups.TryGetValue(top, out List<string>? allNested)) {
                    HashSet<string> exclSet = exclNested != null
                        ? new HashSet<string>(exclNested, StringComparer.OrdinalIgnoreCase)
                        : [];
                    inclNestedList = allNested.Except(exclSet, StringComparer.OrdinalIgnoreCase).ToList();
                }

                effectiveGrouped[top] = inclNestedList?.Count > 0 ? inclNestedList : null;
            }
            else {
                var inclNested = new HashSet<string>(includedGrouped[top]!, StringComparer.OrdinalIgnoreCase);
                if (exclNested != null) {
                    var exclSet = new HashSet<string>(exclNested, StringComparer.OrdinalIgnoreCase);
                    inclNested.ExceptWith(exclSet);
                }

                effectiveGrouped[top] = inclNested.Count > 0 ? [.. inclNested] : null;
            }
        }

        if (effectiveGrouped.Count == 0) {
            error = "No fields remain after applying exclusions.";
            return false;
        }

        projection = ProjectionCompiler.BuildProjection(orderedTops, effectiveGrouped, config);
        return true;
    }

    public static bool TryCreateFullProjection<TEntity, TDto>(
        DtoMappingConfiguration<TEntity, TDto> config,
        out Expression<Func<TEntity, object>> projection,
        out string? error) where TDto : class {
        projection = null!;
        error = null;

        Dictionary<string, List<string>?> grouped = config.GetFullGrouped();
        if (grouped.Count == 0) {
            error = "No mappings defined in configuration.";
            return false;
        }

        Type dtoType = typeof(TDto);
        var allTops = config.GetAllTopLevelFields(dtoType).ToList();
        var orderedTops = allTops.Where(grouped.ContainsKey).ToList();

        projection = ProjectionCompiler.BuildProjection(orderedTops, grouped, config);
        return true;
    }
}
