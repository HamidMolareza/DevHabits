using System.Collections.Concurrent;
using System.Linq.Expressions;
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

/// <summary>
/// Parses field tokens from a comma-separated string.
/// </summary>
internal static class TokenParser {
    /// <summary>
    /// Parses a comma-separated field string into an array of tokens.
    /// </summary>
    /// <param name="fields">The field string.</param>
    /// <returns>Array of field tokens.</returns>
    /// <example>
    /// <code>
    /// var tokens = TokenParser.Parse("Name,Age");
    /// // tokens = ["Name", "Age"]
    /// </code>
    /// </example>
    public static string[] Parse(string fields) {
        return fields.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .ToArray();
    }
}

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

/// <summary>
/// Compiles accessor functions for DTO properties.
/// </summary>
internal static class AccessorCompiler {
    /// <summary>
    /// Builds accessor functions for top-level and nested DTO properties.
    /// </summary>
    /// <typeparam name="TDto">DTO type.</typeparam>
    /// <param name="grouped">Grouped top-level and nested fields.</param>
    /// <param name="dtoProps">DTO property map.</param>
    /// <param name="error">Error message if compilation fails.</param>
    /// <returns>List of accessor functions, or null if invalid.</returns>
    /// <example>
    /// <code>
    /// var accessors = AccessorCompiler.BuildTopAccessors&lt;UserDto&gt;(grouped, FieldSelector.GetPropertiesMap(typeof(UserDto)), out var error);
    /// // accessors[0].getter(userDto) returns the requested field value
    /// </code>
    /// </example>
    public static List<(string requestedKey, Func<TDto, object> getter)>? BuildTopAccessors<TDto>(
        Dictionary<string, List<string>> grouped,
        Dictionary<string, PropertyInfo> dtoProps,
        out string? error) {
        error = null;

        // For each top-level prop we will create an accessor:
        // either Func<TDto, object> that returns full value,
        // or Func<TDto, IDictionary<string,object?>> returning a dictionary with requested nested subfields.
        var topAccessors = new List<(string requestedKey, Func<TDto, object> getter)>();
        ParameterExpression param = Expression.Parameter(typeof(TDto), "dto");

        foreach ((string topName, var nestedList) in grouped) {
            PropertyInfo topProp = dtoProps[topName];

            if (nestedList == null!) {
                // whole top-level property requested -> compile a getter for topProp
                var getExpr = Expression.Lambda<Func<TDto, object>>(
                    Expression.Convert(Expression.Property(param, topProp), typeof(object)),
                    param);
                Func<TDto, object> compiled = getExpr.Compile();
                topAccessors.Add((topName, compiled));
                continue;
            }

            // nestedList is not null: need to validate nested properties on nested type
            Type nestedType = topProp.PropertyType;
            // if nullable, get the underlying type
            if (nestedType.IsGenericType && nestedType.GetGenericTypeDefinition() == typeof(Nullable<>)) {
                nestedType = Nullable.GetUnderlyingType(nestedType)!;
            }

            Dictionary<string, PropertyInfo> nestedProps = FieldSelector.GetPropertiesMap(nestedType);
            HashSet<string>? nestedFields =
                FieldValidator.ValidateNestedFields(nestedList, nestedProps, nestedType, topName, out error);
            if (nestedFields == null) {
                return null;
            }

            // compile nested getters
            List<(string nestedName, Func<TDto, object?> getter)> nestedGetters =
                CompileNestedGetters<TDto>(nestedFields, param, topProp, nestedProps);

            topAccessors.Add((topName, Accessor));
            continue;

            // local function to capture topProp and nestedGetters
            object Accessor(TDto dto) {
                object? topVal = topProp.GetValue(dto);
                if (topVal == null) {
                    return null!;
                }

                var dict = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                foreach ((string nestedName, Func<TDto, object?> getter) in nestedGetters) {
                    dict[nestedName] = getter(dto);
                }

                return dict;
            }
        }

        return topAccessors;
    }

    /// <summary>
    /// Compiles getter functions for nested DTO properties.
    /// </summary>
    /// <typeparam name="TDto">DTO type.</typeparam>
    /// <param name="nestedFields">Set of nested field names.</param>
    /// <param name="param">Parameter expression for DTO.</param>
    /// <param name="topProp">Top-level property info.</param>
    /// <param name="nestedProps">Nested property map.</param>
    /// <returns>List of nested property getter functions.</returns>
    /// <example>
    /// <code>
    /// var nestedGetters = AccessorCompiler.CompileNestedGetters&lt;UserDto&gt;(fields, param, topProp, nestedProps);
    /// // nestedGetters[0].getter(userDto) returns the nested property value
    /// </code>
    /// </example>
    private static List<(string nestedName, Func<TDto, object?> getter)> CompileNestedGetters<TDto>(
        HashSet<string> nestedFields,
        ParameterExpression param,
        PropertyInfo topProp,
        Dictionary<string, PropertyInfo> nestedProps) {
        // For the nested fields under this top-level, compile getters for each nested property (with null-check).
        // Build nested getters: Func<TDto, object?> that checks topProp != null then returns nestedProp value cast to object.
        var nestedGetters = new List<(string nestedName, Func<TDto, object?> getter)>();

        foreach (string nestedName in nestedFields) {
            PropertyInfo nestedPropInfo = nestedProps[nestedName];

            MemberExpression topPropAccess = Expression.Property(param, topProp);
            BinaryExpression checkNull =
                Expression.Equal(topPropAccess, Expression.Constant(null, topProp.PropertyType));

            MemberExpression nestedAccess = Expression.Property(topPropAccess, nestedPropInfo);
            UnaryExpression converted = Expression.Convert(nestedAccess, typeof(object));

            ConditionalExpression conditional = Expression.Condition(checkNull,
                Expression.Constant(null, typeof(object)),
                converted);

            var lambda = Expression.Lambda<Func<TDto, object?>>(conditional, param);
            Func<TDto, object?> compiled = lambda.Compile();
            nestedGetters.Add((nestedName, compiled));
        }

        return nestedGetters;
    }
}

/// <summary>
/// Factory for creating selector functions from accessors.
/// </summary>
internal static class SelectorFactory {
    /// <summary>
    /// Creates a selector function that returns a dictionary of requested fields from a DTO.
    /// </summary>
    /// <typeparam name="TDto">DTO type.</typeparam>
    /// <param name="tokens">Field tokens.</param>
    /// <param name="topAccessors">Accessor functions for top-level fields.</param>
    /// <returns>Selector function.</returns>
    /// <example>
    /// <code>
    /// var selector = SelectorFactory.Create&lt;UserDto&gt;(new[] { "Name" }, accessors);
    /// var result = selector(new UserDto { Name = "Bob" }); // result["Name"] == "Bob"
    /// </code>
    /// </example>
    public static Func<TDto, object> Create<TDto>(
        string[] tokens,
        List<(string requestedKey, Func<TDto, object> getter)> topAccessors) {
        return dto => {
            // handle null or default dto
            if (Equals(dto, default(TDto)))
                return null!;

            var result = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            // preserve order of original tokens (first occurrence wins) and skip duplicates
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (string token in tokens) {
                string[] parts = token.Split('.',
                    StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (parts.Length == 0) {
                    continue;
                }

                string top = parts[0];
                if (!seen.Add(top))
                    continue;

                // find the accessor for this top-level property
                (string _, Func<TDto, object> getter) =
                    topAccessors.FirstOrDefault(a => a.requestedKey.Equals(top, StringComparison.OrdinalIgnoreCase));
                if (getter != null)
                    result[top] = getter(dto);
            }

            return result;
        };
    }
}
