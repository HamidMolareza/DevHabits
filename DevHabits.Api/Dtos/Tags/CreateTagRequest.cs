namespace DevHabits.Api.Dtos.Tags;

public sealed record CreateTagRequest {
    public required string Name { get; set; }
    public string? Description { get; set; }
}
