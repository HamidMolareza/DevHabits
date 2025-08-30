using Microsoft.Extensions.Primitives;

namespace DevHabits.Api.Shared.Libraries.Hateoas;

public static class HateoasExtensions {
    public static bool WantsHateoas(this HttpRequest request, string applicationName) =>
        request.Headers.TryGetValue("Accept", out StringValues acceptHeader) && acceptHeader.Any(h =>
            h is not null && h.Contains($"application/vnd.{applicationName}.hateoas+json",
                StringComparison.OrdinalIgnoreCase));
}
