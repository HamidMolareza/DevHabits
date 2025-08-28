using System.Collections.Concurrent;
using System.Reflection;

namespace DevHabits.Api.Shared.Libraries.DataShaping;

/// <summary>
/// Provides utilities for selecting fields from DTOs dynamically based on a field string.
/// </summary>
public static class FieldSelector {
    // Cache property dictionaries per type to avoid repeated reflection
    private static readonly ConcurrentDictionary<Type, Dictionary<string, PropertyInfo>> PropsCache = new();

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
        out string? error) {
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
        Dictionary<string, PropertyInfo> dtoProps = GetPropertiesMap(dtoType);

        // structure: topField -> list of nested fields (without top prefix)
        Dictionary<string, List<string>>? grouped =
            FieldValidator.ValidateTopLevelFields(tokens, dtoProps, dtoType, out error);
        if (grouped == null)
            return false;

        List<(string requestedKey, Func<TDto, object> getter)>? topAccessors =
            AccessorCompiler.BuildTopAccessors<TDto>(grouped, dtoProps, out error);
        if (topAccessors == null) {
            return false;
        }

        selector = SelectorFactory.Create(tokens, topAccessors);
        return true;
    }

    /// <summary>
    /// Gets a dictionary mapping property names to PropertyInfo for the given type.
    /// </summary>
    /// <param name="type">The type to inspect.</param>
    /// <returns>Dictionary of property names to PropertyInfo.</returns>
    /// <example>
    /// <code>
    /// var map = FieldSelector.GetPropertiesMap(typeof(UserDto));
    /// // map["Name"] gives PropertyInfo for Name property
    /// </code>
    /// </example>
    public static Dictionary<string, PropertyInfo> GetPropertiesMap(Type type) {
        return PropsCache.GetOrAdd(type, t => {
            var dict = t.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .ToDictionary(p => p.Name, p => p, StringComparer.OrdinalIgnoreCase);
            return dict;
        });
    }
}
