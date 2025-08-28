 using System.Linq.Expressions;
using System.Reflection;

namespace DevHabits.Api.Shared.Libraries.DataShaping;

/// <summary>
/// Configuration for mapping entity properties to DTO fields with expressions.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
/// <typeparam name="TDto">The DTO type for type-safe property selection.</typeparam>
public class DtoMappingConfiguration<TEntity, TDto> where TDto : class {
    internal readonly Dictionary<string, Expression<Func<TEntity, object?>>> Mappings =
        new(StringComparer.OrdinalIgnoreCase);

    internal readonly Dictionary<string, List<string>> NestedGroups = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Maps a DTO property to an entity expression.
    /// </summary>
    /// <typeparam name="TProp">The property type.</typeparam>
    /// <param name="dtoSelector">Selector for the DTO property (e.g., dto => dto.Profile.Email).</param>
    /// <param name="entitySelector">Selector for the entity value (e.g., entity => entity.Profile.Email).</param>
    public void MapProperty<TProp>(Expression<Func<TDto, TProp>> dtoSelector,
        Expression<Func<TEntity, TProp>> entitySelector) {
        string path = GetPropertyPath(dtoSelector);
        Mappings[path] =
            Expression.Lambda<Func<TEntity, object?>>(Expression.Convert(entitySelector.Body, typeof(object)),
                entitySelector.Parameters);

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

    private static string GetPropertyPath<TProp>(Expression<Func<TDto, TProp>> expr) {
        string path = string.Empty;
        Expression? current = expr.Body;
        while (current is MemberExpression memberExpr) {
            if (!string.IsNullOrEmpty(path))
                path = $".{path}";
            path = $"{memberExpr.Member.Name}{path}";
            current = memberExpr.Expression;
        }

        return path;
    }

    internal Dictionary<string, List<string>?> GetFullGrouped() {
        var grouped = new Dictionary<string, List<string>?>(StringComparer.OrdinalIgnoreCase);

        foreach (string path in Mappings.Keys) {
            string[] parts = path.Split('.');
            string top = parts[0];
            if (parts.Length == 1) {
                grouped[top] = null;
            }
            else if (grouped.TryGetValue(top, out List<string>? value) && value == null) {
                // Already full
            }

            // Handled by _nestedGroups
        }

        foreach (KeyValuePair<string, List<string>> kv in NestedGroups) {
            if (!grouped.ContainsKey(kv.Key)) {
                grouped[kv.Key] = new List<string>(kv.Value); // full nested
            }
        }

        return grouped;
    }

    internal IEnumerable<string> GetAllTopLevelFields(Type dtoType) {
        // Use reflection for order
        PropertyInfo[] allProps = dtoType.GetProperties(BindingFlags.Instance | BindingFlags.Public);
        return allProps.Select(p => p.Name);
    }
}
