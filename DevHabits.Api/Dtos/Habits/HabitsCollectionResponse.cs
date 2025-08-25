namespace DevHabits.Api.Dtos.Habits;

public sealed record HabitsCollectionResponse {
    public required List<HabitResponse> Data { get; init; }
}
