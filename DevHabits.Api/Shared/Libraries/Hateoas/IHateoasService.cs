using Microsoft.Extensions.Primitives;

namespace DevHabits.Api.Shared.Libraries.Hateoas;

public interface IHateoasService {
    T Wrap<T>(HttpRequest request, T data, Action<T> linkBuilder);

    void Wrap<T>(HttpRequest request, List<T> items, Action<T> linkBuilder);
}

public class HateoasService(string applicationName) : IHateoasService {
    private readonly string _hateoasMediaType = $"application/vnd.{applicationName}.hateoas+json";

    public bool WantsHateoas(HttpRequest request) =>
        request.Headers.TryGetValue("Accept", out StringValues acceptHeader) && acceptHeader.Any(h =>
            h is not null && h.Contains(_hateoasMediaType, StringComparison.OrdinalIgnoreCase));


    public T Wrap<T>(HttpRequest request, T data, Action<T> linkBuilder) {
        if (!WantsHateoas(request))
            return data;

        linkBuilder(data);
        return data;
    }

    public void Wrap<T>(HttpRequest request, List<T> items, Action<T> linkBuilder) {
        if (!WantsHateoas(request))
            return;

        foreach (T item in items) {
            linkBuilder(item);
        }
    }
}
