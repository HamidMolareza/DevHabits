using System.Reflection;
using System.Collections.Concurrent;
using System.Linq.Expressions;

namespace DevHabits.Api.Shared.Libraries.DataShaping;

public static class FieldSelector {
    // Cache property dictionaries per type to avoid repeated reflection
    private static readonly ConcurrentDictionary<Type, Dictionary<string, PropertyInfo>> PropsCache =
        new();

    /// <summary>
    /// Parse fields string, validate against DTO type, and return a selector function that
    /// maps dto -> object (IDictionary) containing only requested fields.
    /// Returns false and error message if validation fails.
    /// </summary>
    public static bool TryCreateSelector<TDto>(string? fields,
        out Func<TDto, object> selector,
        out string? error) {
        selector = _ => throw new InvalidOperationException("uninitialized");
        error = null;

        if (string.IsNullOrWhiteSpace(fields)) {
            // If no fields requested, return identity (return the DTO itself)
            selector = dto => dto;
            return true;
        }

        // parse requested tokens: split by ',' and trim
        string[] tokens = fields.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .ToArray();

        if (tokens.Length == 0) {
            selector = dto => dto;
            return true;
        }

        Type dtoType = typeof(TDto);
        Dictionary<string, PropertyInfo> dtoProps = GetPropertiesMap(dtoType);

        // structure: topField -> list of nested fields (without top prefix)
        var grouped = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        foreach (string token in tokens) {
            string[] parts = token.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length == 0)
                continue;

            string top = parts[0];
            if (!dtoProps.TryGetValue(top, out PropertyInfo? _)) {
                error = $"Field '{token}' is not a top-level property of {dtoType.Name}.";
                return false;
            }

            if (parts.Length == 1) {
                // request whole top-level property
                grouped[top] = null!; // null list -> means "whole top-level property requested"
            }
            else {
                if (grouped.TryGetValue(top, out List<string>? existing)) {
                    // already requested whole object; ignore subfields
                    continue;
                }

                existing = [];
                grouped[top] = existing;

                // join rest as subtoken (single-level only accepted for our DTOs; but we can validate recursively)
                string rest = string.Join('.', parts.Skip(1));
                existing.Add(rest);
            }
        }

        // Now validate nested segments exist on nested DTOs and compile getters
        ParameterExpression param = Expression.Parameter(typeof(TDto), "dto");

        // For each top-level prop we will create an accessor:
        // either Func<TDto, object> that returns full value,
        // or Func<TDto, IDictionary<string,object?>> returning a dictionary with requested nested subfields.
        var topAccessors = new List<(string requestedKey, Func<TDto, object> getter)>();

        foreach ((string topName, var nestedList) in grouped) {
            PropertyInfo topProp = dtoProps[topName];

            if (nestedList == null) {
                // whole property requested -> compile a getter for topProp
                var getExpr = Expression.Lambda<Func<TDto, object>>(
                    Expression.Convert(Expression.Property(param, topProp), typeof(object)),
                    param);
                Func<TDto, object> compiled = getExpr.Compile();
                topAccessors.Add((topName, compiled));
                continue;
            }

            // nestedList != null: need to validate nested properties on nested type
            Type nestedType = topProp.PropertyType;
            if (nestedType.IsGenericType && nestedType.GetGenericTypeDefinition() == typeof(Nullable<>))
                nestedType = Nullable.GetUnderlyingType(nestedType)!;

            Dictionary<string, PropertyInfo> nestedProps = GetPropertiesMap(nestedType);

            // group nested parts by direct child property (we only support subfields like 'target' or 'some.nested')
            // For simplicity, we only allow subfields one-level deep under top (e.g. milestone.target).
            // If deeper nesting needed, the same approach can be extended recursively.
            var nestedDirect = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            foreach (string nestedFull in nestedList) {
                string[] nestedParts = nestedFull.Split('.',
                    StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (nestedParts.Length == 0)
                    continue;
                string first = nestedParts[0];
                if (!nestedProps.ContainsKey(first)) {
                    error =
                        $"Nested field '{nestedFull}' is not a property of '{nestedType.Name}' (under top-level '{topName}').";
                    return false;
                }

                // currently we only accept direct nested properties (not deeper) - treat deeper as error or extend
                if (nestedParts.Length > 1) {
                    // you may extend here to support deeper nesting by walking types. For now, reject to keep code simple.
                    error =
                        $"Nested field '{nestedFull}' depth > 1 is not supported in this implementation. Expand if required.";
                    return false;
                }

                if (!nestedDirect.TryGetValue(first, out List<string>? list)) {
                    list = [];
                    nestedDirect[first] = list;
                }

                list.Add(first);
            }

            // For the nested fields under this top-level, compile getters for each nested property (with null-check).
            // Build nested getters: Func<TDto, object?> that checks topProp != null then returns nestedProp value cast to object.
            var nestedGetters = new List<(string nestedName, Func<TDto, object?> getter)>();
            foreach (string nestedName in nestedDirect.Keys) {
                PropertyInfo nestedPropInfo = nestedProps[nestedName];

                // expression: dto => dto.TopProp == null ? null : (object)dto.TopProp.NestedProp
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

            // build accessor that returns a dictionary<string,object?> for this top-level token
            object Accessor(TDto dto) {
                // If the whole nested object is null => return null
                object? topVal = topProp.GetValue(dto);
                if (topVal == null)
                    return null;

                var dict = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                foreach ((string nestedName, Func<TDto, object?> getter) in nestedGetters) {
                    dict[nestedName] = getter(dto);
                }

                return dict;
            }

            topAccessors.Add((topName, Accessor));
        }

        // Final selector: produce IDictionary<string,object?> with keys exactly as requested top-level tokens.
        // Keep original token order as much as possible by using tokens parsed earlier.
        selector = dto => {
            // if selector called with null dto, return null (defensive)
            if (Equals(dto, default(TDto)))
                return null!;

            var result = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

            // preserve order of original tokens (first occurrence wins) and skip duplicates
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string tok in tokens) {
                string[] parts = tok.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (parts.Length == 0)
                    continue;
                string top = parts[0];
                if (!seen.Add(top))
                    continue;

                (string _, Func<TDto, object> getter) =
                    topAccessors.FirstOrDefault(a => a.requestedKey.Equals(top, StringComparison.OrdinalIgnoreCase));
                if (getter != null) {
                    result[top] = getter(dto);
                }
            }

            return result;
        };

        return true;
    }

    private static Dictionary<string, PropertyInfo> GetPropertiesMap(Type type) {
        return PropsCache.GetOrAdd(type, t => {
            var dict = t.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .ToDictionary(p => p.Name, p => p, StringComparer.OrdinalIgnoreCase);
            return dict;
        });
    }
}
