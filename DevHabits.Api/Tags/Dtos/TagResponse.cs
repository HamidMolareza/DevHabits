namespace DevHabits.Api.Tags.Dtos;

public sealed record TagsCollectionResponse {
    public required List<TagResponse> Data { get; set; }
}

public sealed record TagResponse {
    public required string Id { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public required DateTime CreatedAtUtc { get; init; }
    public DateTime? UpdatedAtUtc { get; init; }
}
