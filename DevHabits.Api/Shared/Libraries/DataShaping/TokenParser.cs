namespace DevHabits.Api.Shared.Libraries.DataShaping;

/// <summary>
/// Parses field tokens from a comma-separated string.
/// </summary>
internal static class TokenParser {
    /// <summary>
    /// Parses a comma-separated field string into an array of tokens.
    /// </summary>
    /// <param name="fields">The field string.</param>
    /// <returns>Array of field tokens.</returns>
    /// <example>
    /// <code>
    /// var tokens = TokenParser.Parse("Name,Age");
    /// // tokens = ["Name", "Age"]
    /// </code>
    /// </example>
    public static string[] Parse(string fields) {
        return fields.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Distinct()
            .ToArray();
    }
}
