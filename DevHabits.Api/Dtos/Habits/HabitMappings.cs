using DevHabits.Api.Entities;

namespace DevHabits.Api.Dtos.Habits;

internal static class HabitMappings {
    public static Habit ToEntity(this CreateHabitRequest request) {
        Habit habit = new() {
            Id = $"h_{Guid.CreateVersion7()}",
            Name = request.Name,
            Description = request.Description,
            Type = request.Type,
            Frequency = new Frequency { Type = request.Frequency.Type, TimesPerPeriod = request.Frequency.TimesPerPeriod },
            Target = new Target { Value = request.Target.Value, Unit = request.Target.Unit },
            Status = HabitStatus.Ongoing,
            IsArchived = false,
            EndDate = request.EndDate,
            Milestone = request.Milestone is not null
                ? new Milestone {
                    Target = request.Milestone.Target,
                    Current = 0 // Initialize current progress to 0
                }
                : null,
            CreatedAtUtc = DateTime.UtcNow
        };

        return habit;
    }

    public static void UpdateFromDto(this Habit habit, UpdateHabitRequest request) {
        // Update basic properties
        habit.Name = request.Name;
        habit.Description = request.Description;
        habit.Type = request.Type;
        habit.EndDate = request.EndDate;

        // Update frequency (assuming it's immutable, create new instance)
        habit.Frequency = new Frequency { Type = request.Frequency.Type, TimesPerPeriod = request.Frequency.TimesPerPeriod };

        // Update target
        habit.Target = new Target { Value = request.Target.Value, Unit = request.Target.Unit };

        // Update milestone if provided
        if (request.Milestone != null) {
            habit.Milestone ??= new Milestone(); // Create new if doesn't exist
            habit.Milestone.Target = request.Milestone.Target;
            // Note: We don't update Milestone.Current from DTO to preserve progress
        }

        habit.UpdatedAtUtc = DateTime.UtcNow;
    }
}
