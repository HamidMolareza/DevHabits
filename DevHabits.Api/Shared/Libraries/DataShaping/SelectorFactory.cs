namespace DevHabits.Api.Shared.Libraries.DataShaping;

/// <summary>
/// Factory for creating selector functions from accessors.
/// </summary>
internal static class SelectorFactory {
    /// <summary>
    /// Creates a selector function that returns a dictionary of requested fields from a DTO.
    /// </summary>
    /// <typeparam name="TDto">DTO type.</typeparam>
    /// <param name="tokens">Field tokens.</param>
    /// <param name="topAccessors">Accessor functions for top-level fields.</param>
    /// <returns>Selector function.</returns>
    /// <example>
    /// <code>
    /// var selector = SelectorFactory.Create&lt;UserDto&gt;(new[] { "Name" }, accessors);
    /// var result = selector(new UserDto { Name = "Bob" }); // result["Name"] == "Bob"
    /// </code>
    /// </example>
    public static Func<TDto, object> Create<TDto>(
        string[] tokens,
        List<(string requestedKey, Func<TDto, object> getter)> topAccessors) {
        return dto => {
            // handle null or default dto
            if (Equals(dto, default(TDto)))
                return null!;

            var result = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            // preserve order of original tokens (first occurrence wins) and skip duplicates
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (string token in tokens) {
                string[] parts = token.Split('.',
                    StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (parts.Length == 0) {
                    continue;
                }

                string top = parts[0];
                if (!seen.Add(top))
                    continue;

                // find the accessor for this top-level property
                (string _, Func<TDto, object> getter) =
                    topAccessors.FirstOrDefault(a => a.requestedKey.Equals(top, StringComparison.OrdinalIgnoreCase));
                if (getter != null)
                    result[top] = getter(dto);
            }

            return result;
        };
    }
}
