using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.Primitives;

namespace DevHabits.Api.Shared.Libraries.Hateoas;

public static class HateoasConfigure {
    public static IServiceCollection AddHateoas(this IServiceCollection services, string applicationName) {
        return services.Configure<MvcOptions>(options => {
            SystemTextJsonOutputFormatter? jsonOutputFormatter = options.OutputFormatters
                .OfType<SystemTextJsonOutputFormatter>()
                .FirstOrDefault();

            // Add support for custom media types
            jsonOutputFormatter?.SupportedMediaTypes.Add($"application/vnd.{applicationName}.hateoas+json");

            SystemTextJsonInputFormatter? jsonInputFormatter = options.InputFormatters
                .OfType<SystemTextJsonInputFormatter>()
                .FirstOrDefault();

            jsonInputFormatter?.SupportedMediaTypes.Add($"application/vnd.{applicationName}.hateoas+json");
        });
    }
}

public static class HateoasExtensions {
    public static bool WantsHateoas(this HttpRequest request, string applicationName) =>
        request.Headers.TryGetValue("Accept", out StringValues acceptHeader) && acceptHeader.Any(h =>
            h is not null && h.Contains($"application/vnd.{applicationName}.hateoas+json",
                StringComparison.OrdinalIgnoreCase));
}
