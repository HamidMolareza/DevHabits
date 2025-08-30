namespace DevHabits.Api.Shared.Libraries.Hateoas;

public sealed class LinkDto {
    public required string Href { get; init; }
    public required string Method { get; init; }

    public static LinkDto CreateGet(string href) =>
        new() { Href = href, Method = HttpMethods.Get };

    public static LinkDto CreatePost(string href) =>
        new() { Href = href, Method = HttpMethods.Post };

    public static LinkDto CreatePut(string href) =>
        new() { Href = href, Method = HttpMethods.Put };

    public static LinkDto CreateDelete(string href) =>
        new() { Href = href, Method = HttpMethods.Delete };
}
