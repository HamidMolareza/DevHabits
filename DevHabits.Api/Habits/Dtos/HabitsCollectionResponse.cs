namespace DevHabits.Api.Habits.Dtos;

public sealed record HabitsCollectionResponse {
    public required List<HabitResponse> Data { get; init; }
}
