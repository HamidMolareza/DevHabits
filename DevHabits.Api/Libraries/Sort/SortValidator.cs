namespace DevHabits.Api.Libraries.Sort;

public static class SortValidator {
    /// <summary>
    /// Validates alias-based sort string against allowed fields.
    /// Returns mapped dynamic LINQ string (ready for OrderBy).
    /// Throws ArgumentException if alias not found or direction invalid.
    /// </summary>
    public static string? Validate(string? aliasSort, SortOptions options) {
        if (string.IsNullOrWhiteSpace(aliasSort))
            aliasSort = options.DefaultSortAlias;

        if (string.IsNullOrWhiteSpace(aliasSort))
            return null;

        string[] parts = aliasSort.Split(',', StringSplitOptions.RemoveEmptyEntries);
        var mappedParts = new List<string>();

        foreach (string raw in parts) {
            string[] tokens = raw.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length == 0)
                continue;

            string alias = tokens[0].Trim();
            string dir = tokens.Length > 1 ? tokens[1].Trim().ToLowerInvariant() : "asc";

            if (!options.AliasToProperty.TryGetValue(alias, out string? propPath))
                throw new SortValidationException($"Invalid sort field: {alias}");

            if (dir is not "asc" and not "desc")
                throw new SortValidationException($"Invalid sort direction: {dir}");

            mappedParts.Add($"{propPath} {dir}");
        }

        return mappedParts.Count > 0 ? string.Join(", ", mappedParts) : null;
    }
}
