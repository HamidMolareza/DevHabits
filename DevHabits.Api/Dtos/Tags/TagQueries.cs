using System.Linq.Expressions;
using DevHabits.Api.Entities;

namespace DevHabits.Api.Dtos.Tags;

internal static class TagQueries {
    public static Expression<Func<Tag, TagDto>> ProjectToDto() {
        return t => new TagDto {
            Id = t.Id,
            Name = t.Name,
            Description = t.Description,
            CreatedAtUtc = t.CreatedAtUtc,
            UpdatedAtUtc = t.UpdatedAtUtc
        };
    }
}
