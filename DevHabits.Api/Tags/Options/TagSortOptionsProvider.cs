using DevHabits.Api.Shared.Libraries.Sort;
using DevHabits.Api.Tags.Entities;

namespace DevHabits.Api.Tags.Options;

public sealed class TagSortOptionsProvider : ISortOptionsProvider<Tag> {
    private static SortOptions Options { get; } = new(
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
            ["id"] = nameof(Tag.Id),
            ["name"] = nameof(Tag.Name),
            ["createdAt"] = nameof(Tag.CreatedAtUtc),
            ["updatedAt"] = nameof(Tag.UpdatedAtUtc),
        },
        defaultSortAlias: $"{nameof(Tag.Id)} asc"
    );

    public SortOptions GetOptions() => Options;
}
