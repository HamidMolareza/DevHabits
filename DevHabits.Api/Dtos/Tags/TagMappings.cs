using DevHabits.Api.Entities;

namespace DevHabits.Api.Dtos.Tags;

internal static class TagMappings {
    public static Tag ToEntity(this CreateTagRequest request) {
        Tag habit = new() {
            Id = $"t_{Guid.CreateVersion7()}",
            Name = request.Name,
            Description = request.Description,
            CreatedAtUtc = DateTime.UtcNow
        };

        return habit;
    }

    public static void UpdateFromDto(this Tag tag, UpdateTagRequest request) {
        tag.Name = request.Name;
        tag.Description = request.Description;
        tag.UpdatedAtUtc = DateTime.UtcNow;
    }
}
