using System.Linq.Expressions;

namespace DevHabits.Api.Shared.Libraries.DataShaping;

public static class QueryShapeExtensions {
    public static IQueryable<object> SelectFields<TEntity, TDto>(
        this IQueryable<TEntity> source,
        string? fields,
        DtoMappingConfiguration<TEntity, TDto> config)
        where TDto : class
        where TEntity : class {
        if (!TrySelectFields(source, fields, config, out IQueryable<object> query, out string? error))
            throw new ShapeDataException(error ?? "One or more requested fields are invalid.");

        return query;
    }

    public static bool TrySelectFields<TEntity, TDto>(
        this IQueryable<TEntity> source,
        string? fields,
        DtoMappingConfiguration<TEntity, TDto> config,
        out IQueryable<object> query,
        out string? error)
        where TDto : class
        where TEntity : class {
        if (string.IsNullOrWhiteSpace(fields)) {
            if (!FieldSelector.TryCreateFullProjection(config, out Expression<Func<TEntity, object>> fullProjection,
                    out error)) {
                query = null!;
                return false;
            }

            query = source.Select(fullProjection);
            error = null;
            return true;
        }

        if (!FieldSelector.TryCreateSelectorProjection(fields, config, out Expression<Func<TEntity, object>> projection,
                out error)) {
            query = null!;
            return false;
        }

        query = source.Select(projection);
        error = null;
        return true;
    }

    public static IQueryable<object> ExcludeFields<TEntity, TDto>(
        this IQueryable<TEntity> source,
        string? excludeFields,
        DtoMappingConfiguration<TEntity, TDto> config)
        where TDto : class
        where TEntity : class {
        if (!TryExcludeFields(source, excludeFields, config, out IQueryable<object> query, out string? error))
            throw new ShapeDataException(error ?? "One or more excluded fields are invalid.");

        return query;
    }

    public static bool TryExcludeFields<TEntity, TDto>(
        this IQueryable<TEntity> source,
        string? excludeFields,
        DtoMappingConfiguration<TEntity, TDto> config,
        out IQueryable<object> query,
        out string? error)
        where TDto : class
        where TEntity : class {
        if (string.IsNullOrWhiteSpace(excludeFields)) {
            if (!FieldSelector.TryCreateFullProjection(config, out Expression<Func<TEntity, object>> fullProjection,
                    out error)) {
                query = null!;
                return false;
            }

            query = source.Select(fullProjection);
            error = null;
            return true;
        }

        if (!FieldSelector.TryCreateExcludeProjection(excludeFields, config,
                out Expression<Func<TEntity, object>> projection, out error)) {
            query = null!;
            return false;
        }

        query = source.Select(projection);
        error = null;
        return true;
    }

    public static IQueryable<object> ShapeFields<TEntity, TDto>(
        this IQueryable<TEntity> source,
        string? includeFields,
        string? excludeFields,
        DtoMappingConfiguration<TEntity, TDto> config)
        where TDto : class
        where TEntity : class {
        if (!TryShapeFields(source, includeFields, excludeFields, config, out IQueryable<object> query,
                out string? error))
            throw new ShapeDataException(error ?? "One or more fields are invalid.");

        return query;
    }

    public static bool TryShapeFields<TEntity, TDto>(
        this IQueryable<TEntity> source,
        string? includeFields,
        string? excludeFields,
        DtoMappingConfiguration<TEntity, TDto> config,
        out IQueryable<object> query,
        out string? error)
        where TDto : class
        where TEntity : class {
        if (string.IsNullOrWhiteSpace(includeFields) && string.IsNullOrWhiteSpace(excludeFields)) {
            query = source;
            error = null;
            return true;
        }

        if (string.IsNullOrWhiteSpace(includeFields))
            return source.TryExcludeFields(excludeFields, config, out query, out error);

        if (string.IsNullOrWhiteSpace(excludeFields))
            return source.TrySelectFields(includeFields, config, out query, out error);

        if (!FieldSelector.TryCreateShapeProjection(includeFields, excludeFields, config,
                out Expression<Func<TEntity, object>> projection,
                out error)) {
            query = null!;
            return false;
        }

        query = source.Select(projection);
        error = null;
        return true;
    }
}
