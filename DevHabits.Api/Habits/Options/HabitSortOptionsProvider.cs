using DevHabits.Api.Habits.Entities;
using DevHabits.Api.Shared.Libraries.Sort;

namespace DevHabits.Api.Habits.Options;

public sealed class HabitSortOptionsProvider : ISortOptionsProvider<Habit> {
    private static SortOptions Options { get; } = new(
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
            ["id"] = nameof(Habit.Id),
            ["name"] = nameof(Habit.Name),
            ["isArchived"] = nameof(Habit.IsArchived),
            ["endDate"] = nameof(Habit.EndDate),
            ["frequencyType"] = $"{nameof(Habit.Frequency)}.{nameof(Habit.Frequency.Type)}",
        },
        defaultSortAlias: $"{nameof(Habit.Id)} asc"
    );

    public SortOptions GetOptions() => Options;
}
