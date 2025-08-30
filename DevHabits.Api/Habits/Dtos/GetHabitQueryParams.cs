namespace DevHabits.Api.Habits.Dtos;

public class GetHabitQueryParams {
    public string? Sort { get; init; }
    public string? Fields { get; init; }
    public string? ExcludeFields { get; init; }
}
