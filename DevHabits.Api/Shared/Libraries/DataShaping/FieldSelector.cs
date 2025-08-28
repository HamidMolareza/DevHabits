using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;

namespace DevHabits.Api.Shared.Libraries.DataShaping;

public static class FieldSelector {
    // Cache property dictionaries per type to avoid repeated reflection
    private static readonly ConcurrentDictionary<Type, Dictionary<string, PropertyInfo>> PropsCache = new();

    public static bool TryCreateSelector<TDto>(string? fields,
        out Func<TDto, object> selector,
        out string? error) {
        selector = _ => throw new InvalidOperationException("uninitialized");
        error = null;

        if (string.IsNullOrWhiteSpace(fields)) {
            // If no fields requested, return the DTO itself
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

    public static Dictionary<string, PropertyInfo> GetPropertiesMap(Type type) {
        return PropsCache.GetOrAdd(type, t => {
            var dict = t.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .ToDictionary(p => p.Name, p => p, StringComparer.OrdinalIgnoreCase);
            return dict;
        });
    }
}

internal static class TokenParser {
    public static string[] Parse(string fields) {
        return fields.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .ToArray();
    }
}

internal static class FieldValidator {
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

internal static class AccessorCompiler {
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

internal static class SelectorFactory {
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
