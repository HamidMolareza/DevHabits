using DevHabits.Api.Habits.Entities;

namespace DevHabits.Api.Libraries.Sort;

public sealed class HabitSortOptionsProvider : ISortOptionsProvider<Habit> {
    private static SortOptions Options { get; } = new(
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
            ["id"] = nameof(Habit.Id),
            ["name"] = nameof(Habit.Name),
            ["isArchived"] = nameof(Habit.IsArchived),
            ["endDate"] = nameof(Habit.EndDate),
            ["frequencyType"] = $"{nameof(Habit.Frequency)}.{nameof(Habit.Frequency.Type)}",
        },
        defaultSortAlias: "id asc"
    );

    public SortOptions GetOptions() => Options;
}
