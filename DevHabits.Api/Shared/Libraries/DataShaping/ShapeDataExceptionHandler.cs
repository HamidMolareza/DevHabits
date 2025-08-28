using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace DevHabits.Api.Shared.Libraries.DataShaping;

public sealed class ShapeDataExceptionHandler(
    IProblemDetailsService problemDetailsService) : IExceptionHandler {
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken) {
        if (exception is not ShapeDataException sortException)
            return false; // Let other handlers/middleware handle it

        var problem = new ProblemDetails {
            Title = "Invalid fields parameter",
            Detail = sortException.Message,
            Status = StatusCodes.Status400BadRequest
        };

        httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;

        // ✅ Use ProblemDetailsService so CustomizeProblemDetails runs
        await problemDetailsService.WriteAsync(new ProblemDetailsContext {
            HttpContext = httpContext,
            ProblemDetails = problem
        });

        return true;
    }
}
