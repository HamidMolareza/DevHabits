namespace DevHabits.Api.Shared.Libraries.DataShaping;

/// <summary>
/// Factory for creating selector functions from accessors.
/// </summary>
internal static class SelectorFactory {
    /// <summary>
    /// Creates a selector function that returns a dictionary of requested fields from a DTO.
    /// </summary>
    /// <typeparam name="TDto">DTO type.</typeparam>
    /// <param name="orderedKeys">Ordered list of top-level keys to include.</param>
    /// <param name="topAccessors">Accessor functions for top-level fields.</param>
    /// <returns>Selector function.</returns>
    /// <example>
    /// <code>
    /// var selector = SelectorFactory.Create&lt;UserDto&gt;(new[] { "Name" }, accessors);
    /// var result = selector(new UserDto { Name = "Bob" }); // result["Name"] == "Bob"
    /// </code>
    /// </example>
    public static Func<TDto, object> Create<TDto>(
        IEnumerable<string> orderedKeys,
        List<(string requestedKey, Func<TDto, object> getter)> topAccessors) {
        return dto => {
            // handle null or default dto
            if (Equals(dto, default(TDto)))
                return null!;

            var result = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

            foreach (string key in orderedKeys) {
                (string _, Func<TDto, object> getter) =
                    topAccessors.FirstOrDefault(a => a.requestedKey.Equals(key, StringComparison.OrdinalIgnoreCase));
                if (getter != null) {
                    result[key] = getter(dto);
                }
            }

            return result;
        };
    }
}
