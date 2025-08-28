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
    public static Dictionary<string, List<string>>? ValidateTopLevelFields(
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
            if (!dtoProps.ContainsKey(top)) {
                error = $"Field '{token}' is not a top-level property of {dtoType.Name}.";
                return null;
            }

            if (parts.Length == 1) {
                grouped[top] = null!; // null list -> means "whole top-level property requested"
                continue;
            }

            if (grouped.TryGetValue(top, out List<string>? existing)) {
                if (existing == null!) {
                    // already requested the whole object; ignore subfields
                    continue;
                }
                // else: Another path like "top.subfield" was already requested
            }
            else {
                // first time seeing this top-level property
                existing = [];
                grouped[top] = existing;
            }

            // join rest as subtoken (single-level only accepted for our DTOs; but we can validate recursively)
            string rest = string.Join('.', parts.Skip(1));
            existing.Add(rest);
        }

        error = null;
        return grouped;
    }

    /// <summary>
    /// Validates nested field tokens against nested DTO properties.
    /// </summary>
    /// <param name="nestedList">List of nested field tokens.</param>
    /// <param name="nestedProps">Nested property map.</param>
    /// <param name="nestedType">Nested type.</param>
    /// <param name="topName">Top-level property name.</param>
    /// <param name="error">Error message if validation fails.</param>
    /// <returns>Set of valid nested field names, or null if invalid.</returns>
    /// <example>
    /// <code>
    /// var nestedFields = FieldValidator.ValidateNestedFields(
    ///     new List&lt;string&gt; { "Email" }, FieldSelector.GetPropertiesMap(typeof(ProfileDto)), typeof(ProfileDto), "Profile", out var error);
    /// // nestedFields contains "Email"
    /// </code>
    /// </example>
    public static HashSet<string>? ValidateNestedFields(
        List<string> nestedList,
        Dictionary<string, PropertyInfo> nestedProps,
        Type nestedType,
        string topName,
        out string? error) {
        error = null;
        var nestedFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (string nestedFull in nestedList) {
            string[] nestedParts =
                nestedFull.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (nestedParts.Length == 0) {
                continue;
            }

            string first = nestedParts[0];
            if (!nestedProps.ContainsKey(first)) {
                error =
                    $"Nested field '{nestedFull}' is not a property of '{nestedType.Name}' (under top-level '{topName}').";
                return null;
            }

            // currently we only accept direct nested properties (not deeper) - treat deeper as error or extend
            if (nestedParts.Length > 1) {
                error =
                    $"Nested field '{nestedFull}' depth > 1 is not supported.";
                return null;
            }

            nestedFields.Add(first);
        }

        return nestedFields;
    }
}
