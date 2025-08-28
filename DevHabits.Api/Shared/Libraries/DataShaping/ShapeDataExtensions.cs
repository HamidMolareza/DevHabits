namespace DevHabits.Api.Shared.Libraries.DataShaping;

public static class ShapeDataExtensions {
    public static IEnumerable<object> IncludeFields<TDto>(
        this IEnumerable<TDto> source,
        string? fields) where TDto : class {
        if (!source.TryIncludeFields(fields, out IEnumerable<object>? items, out string? error))
            throw new ShapeDataException(error ?? "One or more requested fields are invalid.");

        return items;
    }

    public static bool TryIncludeFields<TDto>(
        this IEnumerable<TDto> source,
        string? fields,
        out IEnumerable<object>? items,
        out string? error) where TDto : class {
        if (string.IsNullOrWhiteSpace(fields)) {
            items = source.ToList();
            error = null;
            return true;
        }

        if (!FieldSelector.TryCreateIncluder(fields, out Func<TDto, object> selector, out error)) {
            items = null;
            return false;
        }

        items = source.Select(dto => selector(dto));
        error = null;
        return true;
    }

    public static IEnumerable<object> ExcludeFields<TDto>(
        this IEnumerable<TDto> source,
        string? excludeFields) where TDto : class {
        if (!source.TryExcludeFields(excludeFields, out IEnumerable<object>? items, out string? error))
            throw new ShapeDataException(error ?? "One or more excluded fields are invalid.");

        return items;
    }

    public static bool TryExcludeFields<TDto>(
        this IEnumerable<TDto> source,
        string? excludeFields,
        out IEnumerable<object>? items,
        out string? error) where TDto : class {
        if (string.IsNullOrWhiteSpace(excludeFields)) {
            items = source.ToList();
            error = null;
            return true;
        }

        if (!FieldSelector.TryCreateExcluder(excludeFields, out Func<TDto, object> excluder, out error)) {
            items = null;
            return false;
        }

        items = source.Select(dto => excluder(dto));
        error = null;
        return true;
    }

    /// <summary>
    /// Shapes DTOs by including specified fields and excluding others, both required.
    /// </summary>
    /// <typeparam name="TDto">The DTO type.</typeparam>
    /// <param name="source">The source DTO collection.</param>
    /// <param name="includeFields">Comma-separated fields to include.</param>
    /// <param name="excludeFields">Comma-separated fields to exclude.</param>
    /// <returns>Collection of shaped objects.</returns>
    /// <exception cref="ShapeDataException">Thrown if fields are invalid or none remain after exclusion.</exception>
    public static IEnumerable<object> ShapeFields<TDto>(
        this IEnumerable<TDto> source,
        string? includeFields,
        string? excludeFields) where TDto : class {
        if (!source.TryShapeFields(includeFields, excludeFields, out IEnumerable<object>? items, out string? error))
            throw new ShapeDataException(error ?? "One or more fields are invalid.");

        return items;
    }

    /// <summary>
    /// Attempts to shape DTOs by including specified fields and excluding others.
    /// </summary>
    /// <typeparam name="TDto">The DTO type.</typeparam>
    /// <param name="source">The source DTO collection.</param>
    /// <param name="includeFields">Comma-separated fields to include.</param>
    /// <param name="excludeFields">Comma-separated fields to exclude.</param>
    /// <param name="items">Output collection of shaped objects.</param>
    /// <param name="error">Error message if shaping fails.</param>
    /// <returns>True if shaping succeeds; otherwise, false.</returns>
    public static bool TryShapeFields<TDto>(
        this IEnumerable<TDto> source,
        string? includeFields,
        string? excludeFields,
        out IEnumerable<object>? items,
        out string? error) where TDto : class {
        if (string.IsNullOrWhiteSpace(includeFields) && string.IsNullOrWhiteSpace(excludeFields)) {
            items = source.ToList();
            error = null;
            return true;
        }

        if (string.IsNullOrWhiteSpace(includeFields))
            return source.TryExcludeFields(excludeFields, out items, out error);

        if (string.IsNullOrWhiteSpace(excludeFields))
            return source.TryIncludeFields(includeFields, out items, out error);

        if (!FieldSelector.TryCreateShaper(includeFields, excludeFields, out Func<TDto, object> shaper, out error)) {
            items = null;
            return false;
        }

        items = source.Select(dto => shaper(dto));
        error = null;
        return true;
    }
}
