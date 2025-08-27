using DevHabits.Api.HabitTags.Entities;

namespace DevHabits.Api.Tags.Entities;

public sealed class Tag {
    public required string Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }

    public ICollection<HabitTag> HabitTags { get; set; } = null!;
}
