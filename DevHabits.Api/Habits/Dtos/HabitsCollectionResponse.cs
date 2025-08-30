using System.Collections.ObjectModel;
using DevHabits.Api.Shared.Libraries.Hateoas;

namespace DevHabits.Api.Habits.Dtos;

public sealed record HabitsCollectionResponse {
    public required IEnumerable<object> Data { get; init; }
    public ReadOnlyDictionary<string, LinkDto>? Links { get; init; }
}
