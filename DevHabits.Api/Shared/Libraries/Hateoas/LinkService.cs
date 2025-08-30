namespace DevHabits.Api.Shared.Libraries.Hateoas;

public class LinkService(LinkGenerator linkGenerator, IHttpContextAccessor contextAccessor) {
    public LinkDto Create(string endpointName, string method, object? values = null,
        string? controller = null) {
        string? href = linkGenerator.GetUriByAction(
            contextAccessor.HttpContext!,
            endpointName,
            controller,
            values
        );

        return new LinkDto {
            Href = href ?? throw new ArgumentException("Invalid endpoint name provided", nameof(endpointName)),
            Method = method
        };
    }

    public LinkDto CreateGet(string endpointName, object? values = null, string? controller = null) {
        string href = CreateHref(linkGenerator, contextAccessor, endpointName, values, controller)!;
        return LinkDto.CreateGet(href);
    }

    public LinkDto CreatePost(string endpointName, object? values = null, string? controller = null) {
        string href = CreateHref(linkGenerator, contextAccessor, endpointName, values, controller)!;
        return LinkDto.CreatePost(href);
    }

    public LinkDto CreatePut(string endpointName, object? values = null, string? controller = null) {
        string href = CreateHref(linkGenerator, contextAccessor, endpointName, values, controller)!;
        return LinkDto.CreatePut(href);
    }

    public LinkDto CreateDelete(string endpointName, object? values = null, string? controller = null) {
        string href = CreateHref(linkGenerator, contextAccessor, endpointName, values, controller)!;
        return LinkDto.CreateDelete(href);
    }

    private static string? CreateHref(LinkGenerator linkGenerator, IHttpContextAccessor contextAccessor,
        string endpointName, object? values = null, string? controller = null) {
        return linkGenerator.GetUriByAction(
            contextAccessor.HttpContext!,
            endpointName,
            controller,
            values
        );
    }
}
