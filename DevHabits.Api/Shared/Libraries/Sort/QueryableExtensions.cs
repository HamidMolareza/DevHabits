using System.Linq.Dynamic.Core;

namespace DevHabits.Api.Shared.Libraries.Sort;

public static class QueryableExtensions {
    /// <summary>
    /// Apply sorting. Throws if validation fails or sorting cannot be applied.
    /// </summary>
    public static IQueryable<T> ApplySort<T>(this IQueryable<T> query, string? aliasSort, SortOptions options) {
        string? mapped = SortValidator.Validate(aliasSort, options);
        return !string.IsNullOrWhiteSpace(mapped)
            ? query.OrderBy(mapped)
            : query; // nothing to sort
    }

    /// <summary>
    /// Try apply sorting. Returns false if validation or sorting fails.
    /// </summary>
    public static bool TryApplySort<T>(this IQueryable<T> query, string? aliasSort, SortOptions options,
        out IQueryable<T> result) {
        result = query;
        try {
            string? mapped = SortValidator.Validate(aliasSort, options);
            if (!string.IsNullOrWhiteSpace(mapped)) {
                result = query.OrderBy(mapped);
            }

            return true;
        }
        catch {
            return false;
        }
    }
}
