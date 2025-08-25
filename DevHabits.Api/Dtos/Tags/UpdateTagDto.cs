namespace DevHabits.Api.Dtos.Tags;

public sealed record UpdateTagDto {
    public string Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
}
