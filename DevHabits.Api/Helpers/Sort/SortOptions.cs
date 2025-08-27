namespace DevHabits.Api.Helpers.Sort;

public class SortOptions(IDictionary<string, string> aliasToProperty, string? defaultSortAlias = null) {
    /// <summary>
    /// Alias -> Entity property mapping (case-insensitive).
    /// Example: "name" => "Name", "frequencyType" => "Frequency.Type"
    /// </summary>
    public IReadOnlyDictionary<string, string> AliasToProperty { get; } = new Dictionary<string, string>(aliasToProperty, StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Default sort (in alias syntax). Example: "id asc".
    /// Can be null.
    /// </summary>
    public string? DefaultSortAlias { get; } = defaultSortAlias;
}
