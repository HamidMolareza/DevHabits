using DevHabits.Api.Habits.Entities;
using DevHabits.Api.Tags.Entities;

namespace DevHabits.Api.HabitTags.Entities;

public class HabitTag {
    public required string HabitId { get; set; }
    public Habit Habit { get; set; } = null!;
    public required string TagId { get; set; }
    public Tag Tag { get; set; } = null!;

    public required DateTime CreatedDateTime { get; set; }
}
