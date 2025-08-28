using System.Linq.Expressions;
using System.Reflection;

namespace DevHabits.Api.Shared.Libraries.DataShaping;

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
