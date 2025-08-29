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
            // 🔹 Handle collections
            if (config.CollectionMappings.TryGetValue(topName, out CollectionMapping? collectionMapping)) {
                Expression collectionExpr = RebindExpression(collectionMapping.EntitySelector, entity);

                // Build selector for each element
                dynamic nestedConfigObj = collectionMapping.NestedConfig;
                Type entityItemType = collectionMapping.EntitySelector.ReturnType.GetGenericArguments()[0];
                ParameterExpression itemParam = Expression.Parameter(entityItemType, "item");

                dynamic? nestedDictInit = BuildNestedProjection(itemParam, nestedConfigObj, grouped[topName]);

                // item => (object)new Dictionary<string, object?> {...}
                dynamic? selectorLambda = Expression.Lambda(
                    Expression.Convert(nestedDictInit, typeof(object)),
                    itemParam);

                // collection.Select(selector).ToList()
                MethodInfo selectMethod = typeof(Enumerable)
                    .GetMethods(BindingFlags.Public | BindingFlags.Static)
                    .Where(m => m.Name == "Select" && m.GetParameters().Length == 2)
                    .Select(m => new { Method = m, Params = m.GetParameters() })
                    .Where(x =>
                        x.Params[1].ParameterType.IsGenericType &&
                        x.Params[1].ParameterType.GetGenericTypeDefinition() == typeof(Func<,>))
                    .Select(x => x.Method)
                    .Single()
                    .MakeGenericMethod(itemParam.Type, typeof(object));

                MethodInfo toListMethod = typeof(Enumerable)
                    .GetMethods()
                    .Single(m => m.Name == "ToList" && m.GetParameters().Length == 1)
                    .MakeGenericMethod(typeof(object));

                dynamic? selectCall = Expression.Call(selectMethod, collectionExpr, selectorLambda);
                dynamic? toListCall = Expression.Call(toListMethod, selectCall);

                initializers.Add(Expression.ElementInit(addMethod,
                    Expression.Constant(topName),
                    Expression.Convert(toListCall, typeof(object))));
                continue;
            }

            // 🔹 Otherwise: scalar or nested
            if (!grouped.TryGetValue(topName, out List<string>? nestedList))
                continue;

            List<string>? effectiveNested = nestedList;

            if (nestedList == null && config.NestedGroups.TryGetValue(topName, out List<string>? allNested)) {
                effectiveNested = allNested; // treat as all nested
            }

            if (effectiveNested == null) {
                // Primitive
                string path = topName;
                if (!config.Mappings.TryGetValue(path, out Expression<Func<TEntity, object?>>? mapExpr)) {
                    throw new InvalidOperationException($"No mapping defined for '{path}'.");
                }

                Expression valueExpr = RebindExpression(mapExpr, entity);
                initializers.Add(Expression.ElementInit(addMethod, Expression.Constant(topName), valueExpr));
            }
            else {
                // Nested object
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
                        // unwrap null-forgiving, convert, etc.
                        static Expression Unwrap(Expression e) {
                            while (true) {
                                switch (e) {
                                    case UnaryExpression u:
                                        e = u.Operand;
                                        continue;
                                    case MemberExpression { Expression: not null } m:
                                        return m.Expression; // parent (e.g. entity.Milestone)
                                    default:
                                        break;
                                }

                                break;
                            }

                            return e;
                        }

                        Expression? parent = Unwrap(mapExpr.Body);
                        if (parent != null!) {
                            Expression parentExpr = RebindExpression(
                                Expression.Lambda(parent, mapExpr.Parameters[0]),
                                entity);

                            nullCheck = Expression.Equal(
                                Expression.Convert(parentExpr, typeof(object)),
                                Expression.Constant(null, typeof(object)));
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

    /// <summary>
    /// Builds a nested dictionary projection for collection items.
    /// </summary>
    private static ListInitExpression BuildNestedProjection<TNestedEntity, TNestedDto>(
        ParameterExpression itemParam,
        DtoMappingConfiguration<TNestedEntity, TNestedDto> nestedConfig,
        List<string> requestedNestedTops)
        where TNestedDto : class {
        Type dictType = typeof(Dictionary<string, object?>);
        MethodInfo addMethod = dictType.GetMethod("Add", [typeof(string), typeof(object)])!;

        var nestedInits = new List<ElementInit>();

        IEnumerable<KeyValuePair<string, Expression<Func<TNestedEntity, object?>>>> targetMapping =
            nestedConfig.Mappings.Where(nc => requestedNestedTops.Contains(nc.Key, StringComparer.OrdinalIgnoreCase));

        foreach ((string nestedName, Expression<Func<TNestedEntity, object?>> expression) in targetMapping) {
            Expression valueExpr = RebindExpression(expression, itemParam);
            nestedInits.Add(Expression.ElementInit(addMethod,
                Expression.Constant(nestedName),
                valueExpr));
        }

        NewExpression nestedDictNew = Expression.New(dictType);
        return Expression.ListInit(nestedDictNew, nestedInits);
    }

    private static Expression RebindExpression(LambdaExpression expr, ParameterExpression newParam) {
        var visitor = new RebindVisitor(expr.Parameters[0], newParam);
        return visitor.Visit(expr.Body)!;
    }

    private sealed class RebindVisitor : ExpressionVisitor {
        private readonly ParameterExpression _oldParam;
        private readonly ParameterExpression _newParam;

        public RebindVisitor(ParameterExpression oldParam, ParameterExpression newParam) {
            _oldParam = oldParam;
            _newParam = newParam;
        }

        protected override Expression VisitParameter(ParameterExpression node)
            => node == _oldParam ? _newParam : base.VisitParameter(node);
    }
}
