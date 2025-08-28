namespace DevHabits.Api.Habits.Dtos;

public sealed record HabitsCollectionResponse {
    public required IEnumerable<object> Data { get; init; }
}
