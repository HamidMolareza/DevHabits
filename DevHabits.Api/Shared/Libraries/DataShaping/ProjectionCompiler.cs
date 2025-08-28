using System.Linq.Expressions;
using System.Reflection;

namespace DevHabits.Api.Shared.Libraries.DataShaping;

/// <summary>
/// Compiles projection expressions for entity to shaped object.
/// </summary>
internal static class ProjectionCompiler {
    public static Expression<Func<TEntity, object>> BuildProjection<TEntity, TDto>(
        IEnumerable<string> orderedTops,
        Dictionary<string, List<string>?> grouped,
        DtoMappingConfiguration<TEntity, TDto> config) where TDto : class {
        ParameterExpression entity = Expression.Parameter(typeof(TEntity), "entity");

        Type dictType = typeof(Dictionary<string, object?>);
        MethodInfo addMethod = dictType.GetMethod("Add", [typeof(string), typeof(object)])!;

        NewExpression dictNew = Expression.New(dictType);
        var initializers = new List<ElementInit>();

        foreach (string topName in orderedTops) {
            if (!grouped.TryGetValue(topName, out List<string>? nestedList))
                continue;

            List<string>? effectiveNested = nestedList;

            if (nestedList == null && config.NestedGroups.TryGetValue(topName, out List<string>? allNested)) {
                // Full complex, treat as all nested
                effectiveNested = allNested;
            }

            if (effectiveNested == null) {
                // Primitive or full simple
                string path = topName;
                if (!config.Mappings.TryGetValue(path, out Expression<Func<TEntity, object?>>? mapExpr)) {
                    throw new InvalidOperationException($"No mapping defined for '{path}'.");
                }

                Expression valueExpr = RebindExpression(mapExpr, entity);
                initializers.Add(Expression.ElementInit(addMethod, Expression.Constant(topName), valueExpr));
            }
            else {
                // Nested (partial or full)
                var nestedInits = new List<ElementInit>();
                Expression? nullCheck = null;

                foreach (string nestedName in effectiveNested) {
                    string path = topName + "." + nestedName;
                    if (!config.Mappings.TryGetValue(path, out Expression<Func<TEntity, object?>>? mapExpr)) {
                        throw new InvalidOperationException($"No mapping defined for '{path}'.");
                    }

                    Expression valueExpr = RebindExpression(mapExpr, entity);
                    nestedInits.Add(Expression.ElementInit(addMethod, Expression.Constant(nestedName), valueExpr));

                    if (nullCheck == null) {
                        Expression body = mapExpr.Body;
                        if (body is UnaryExpression ue)
                            body = ue.Operand;
                        if (body is MemberExpression me) {
                            Expression parentExpr =
                                RebindExpression(
                                    Expression.Lambda<Func<TEntity, object?>>(me.Expression!, mapExpr.Parameters[0]),
                                    entity);
                            nullCheck = Expression.Equal(parentExpr, Expression.Constant(null, typeof(object)));
                        }
                        else {
                            throw new InvalidOperationException(
                                $"Complex expression not supported for nested path '{path}'.");
                        }
                    }
                }

                NewExpression nestedDictNew = Expression.New(dictType);
                ListInitExpression nestedListInit = Expression.ListInit(nestedDictNew, nestedInits);

                ConditionalExpression conditional = Expression.Condition(nullCheck!,
                    Expression.Constant(null, typeof(object)),
                    Expression.Convert(nestedListInit, typeof(object)));

                initializers.Add(Expression.ElementInit(addMethod, Expression.Constant(topName), conditional));
            }
        }

        ListInitExpression listInit = Expression.ListInit(dictNew, initializers);
        var lambda = Expression.Lambda<Func<TEntity, object>>(Expression.Convert(listInit, typeof(object)), entity);
        return lambda;
    }

    private static Expression RebindExpression<TEntity>(Expression<Func<TEntity, object?>> expr,
        ParameterExpression newParam) {
        var visitor = new RebindVisitor(expr.Parameters[0], newParam);
        return visitor.Visit(expr.Body);
    }

    private sealed class RebindVisitor(ParameterExpression oldParam, ParameterExpression newParam) : ExpressionVisitor {
        protected override Expression VisitParameter(ParameterExpression node) {
            return node == oldParam ? newParam : base.VisitParameter(node);
        }
    }
}
