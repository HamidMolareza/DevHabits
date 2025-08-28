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

        if (!FieldSelector.TryCreateSelector(fields, out Func<TDto, object> selector, out error)) {
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
}
