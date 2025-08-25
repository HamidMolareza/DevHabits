namespace DevHabits.Api.Dtos.Tags;

public sealed record CreateTagDto {
    public required string Name { get; set; }
    public string? Description { get; set; }
}
