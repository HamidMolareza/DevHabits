using System.Linq.Expressions;
using System.Reflection;

namespace DevHabits.Api.Shared.Libraries.DataShaping;

/// <summary>
/// Configuration for mapping entity properties to DTO fields with expressions.
/// Supports both scalar and collection mappings.
/// </summary>
public class DtoMappingConfiguration<TEntity, TDto>
    where TDto : class {
    internal readonly Dictionary<string, Expression<Func<TEntity, object?>>> Mappings =
        new(StringComparer.OrdinalIgnoreCase);

    internal readonly Dictionary<string, List<string>> NestedGroups =
        new(StringComparer.OrdinalIgnoreCase);

    // ✅ new: store collection mappings separately
    internal readonly Dictionary<string, CollectionMapping> CollectionMappings =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Maps a scalar DTO property to an entity expression.
    /// </summary>
    public void Map<TProp>(
        Expression<Func<TDto, TProp>> dtoSelector,
        Expression<Func<TEntity, TProp>> entitySelector) {
        string path = GetPropertyPath(dtoSelector);

        Mappings[path] = Expression.Lambda<Func<TEntity, object?>>(
            Expression.Convert(entitySelector.Body, typeof(object)),
            entitySelector.Parameters);

        RegisterNested(path);
    }

    public DtoMappingConfiguration<TEntity, TDto> MapComplex<TSubDto, TSubEntity>(
        Expression<Func<TDto, TSubDto>> dtoSelector,
        Expression<Func<TEntity, TSubEntity>> entitySelector,
        Action<DtoMappingConfiguration<TSubEntity, TSubDto>> subConfig)
        where TSubDto : class
        where TSubEntity : class {
        // Create a sub-configuration for the nested type
        var nestedConfig = new DtoMappingConfiguration<TSubEntity, TSubDto>();
        subConfig(nestedConfig);

        string prefix = GetPropertyPath(dtoSelector);

        // Register all nested paths with prefix
        foreach (KeyValuePair<string, Expression<Func<TSubEntity, object?>>> kvp in nestedConfig.Mappings) {
            string nestedPath = $"{prefix}.{kvp.Key}";

            // Rebind the inner entity expression to the outer entity parameter
            ParameterExpression param = entitySelector.Parameters[0];
            InvocationExpression body = Expression.Invoke(kvp.Value, Expression.Invoke(entitySelector, param));
            var rebasedExpr = Expression.Lambda<Func<TEntity, object?>>(body, param);

            Mappings[nestedPath] = rebasedExpr;
        }

        return this;
    }


    /// <summary>
    /// Maps a collection DTO property to an entity collection, with nested configuration.
    /// </summary>
    public void MapCollection<TDtoItem, TEntityItem>(
        Expression<Func<TDto, IEnumerable<TDtoItem>>> dtoSelector,
        Expression<Func<TEntity, IEnumerable<TEntityItem>>> entitySelector,
        Action<DtoMappingConfiguration<TEntityItem, TDtoItem>> nestedConfig)
        where TDtoItem : class
        where TEntityItem : class {
        string path = GetPropertyPath(dtoSelector);

        var nested = new DtoMappingConfiguration<TEntityItem, TDtoItem>();
        nestedConfig(nested);

        CollectionMappings[path] = new CollectionMapping(entitySelector, nested);

        RegisterNested(path);
    }

    private static string GetPropertyPath<TProp>(Expression<Func<TDto, TProp>> expr) {
        string path = string.Empty;
        Expression? current = expr.Body;

        while (current != null) {
            switch (current) {
                case MemberExpression memberExpr:
                    if (!string.IsNullOrEmpty(path))
                        path = $".{path}";
                    path = $"{memberExpr.Member.Name}{path}";
                    current = memberExpr.Expression;
                    break;

                case MethodCallExpression methodCallExpr
                    when methodCallExpr.Method.Name == "get_Item" && methodCallExpr.Object != null:
                    current = methodCallExpr.Object; // skip the [index]
                    break;

                case UnaryExpression unaryExpr:
                    current = unaryExpr.Operand;
                    break;

                default:
                    current = null;
                    break;
            }
        }

        return path;
    }

    private void RegisterNested(string path) {
        string[] parts = path.Split('.');
        if (parts.Length < 2)
            return;

        string top = parts[0];
        string nested = parts[1];
        if (!NestedGroups.TryGetValue(top, out List<string>? list) && list is null) {
            list = [];
            NestedGroups[top] = list;
        }

        if (!list.Contains(nested, StringComparer.OrdinalIgnoreCase)) {
            list.Add(nested);
        }
    }

    internal Dictionary<string, List<string>?> GetFullGrouped() {
        var grouped = new Dictionary<string, List<string>?>(StringComparer.OrdinalIgnoreCase);

        foreach (string path in Mappings.Keys) {
            string[] parts = path.Split('.');
            string top = parts[0];
            if (parts.Length == 1) {
                grouped[top] = null;
            }
        }

        foreach (KeyValuePair<string, List<string>> kv in NestedGroups) {
            if (!grouped.ContainsKey(kv.Key)) {
                grouped[kv.Key] = new List<string>(kv.Value);
            }
        }

        return grouped;
    }

    internal IEnumerable<string> GetAllTopLevelFields(Type dtoType) {
        return dtoType.GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Select(p => p.Name);
    }
}

/// <summary>
/// Holds collection mapping metadata.
/// </summary>
internal sealed record CollectionMapping(
    LambdaExpression EntitySelector,
    object NestedConfig // actually DtoMappingConfiguration<TEntityItem, TDtoItem>
);
