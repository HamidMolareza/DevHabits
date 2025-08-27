using System.Linq.Expressions;
using DevHabits.Api.Tags.Entities;

namespace DevHabits.Api.Tags.Dtos;

internal static class TagQueries {
    public static Expression<Func<Tag, TagResponse>> ProjectToDto() {
        return t => new TagResponse {
            Id = t.Id,
            Name = t.Name,
            Description = t.Description,
            CreatedAtUtc = t.CreatedAtUtc,
            UpdatedAtUtc = t.UpdatedAtUtc
        };
    }
}
