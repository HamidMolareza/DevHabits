namespace DevHabits.Api.Shared.Libraries.DataShaping;

public static class ShapeDataExtensions {
    public static IEnumerable<object> ShapeData<TDto>(
        this IEnumerable<TDto> source,
        string? fields) where TDto : class {
        if (!source.TryShapeData(fields, out IEnumerable<object>? items, out string? error))
            throw new ShapeDataException(error ?? "One or more requested fields are invalid.");

        return items;
    }

    public static bool TryShapeData<TDto>(
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
}
