using DevHabits.Api.Libraries.BaseApiControllers.CustomProblemDetails;
using Microsoft.AspNetCore.Mvc;

namespace DevHabits.Api.Libraries.BaseApiControllers;

[ApiController]
public abstract partial class BaseApiController : ControllerBase {
    protected ActionResult NotFoundProblem(string? detail = null, string? resource = null, string? resourceId = null) {
        var problem = new NotFoundProblemDetails {
            Status = StatusCodes.Status404NotFound,
            Title = "Not Found",
            Detail = detail ?? "The requested resource was not found.",
            Resource = resource,
            ResourceId = resourceId
        };
        return Problem(
            statusCode: problem.Status,
            title: problem.Title,
            detail: problem.Detail,
            instance: HttpContext.Request.Path,
            extensions: problem.Extensions
        );
    }

    protected ActionResult ConflictProblem(string? detail = null, string? conflictWithId = null,
        string? currentValue = null) {
        var problem = new ConflictProblemDetails {
            Status = StatusCodes.Status409Conflict,
            Title = "Conflict",
            Detail = detail ??
                     "The request could not be completed due to a conflict with the current state of the resource.",
            ConflictWithId = conflictWithId,
            CurrentValue = currentValue
        };
        return Problem(
            statusCode: problem.Status,
            title: problem.Title,
            detail: problem.Detail,
            instance: HttpContext.Request.Path,
            extensions: problem.Extensions
        );
    }

    protected ActionResult BadRequestProblem(string? detail = null,
        IDictionary<string, string[]>? invalidParams = null) {
        var problem = new BadRequestProblemDetails {
            Status = StatusCodes.Status400BadRequest,
            Title = "Bad Request",
            Detail = detail ?? "The request could not be understood or was missing required parameters.",
            InvalidParams = invalidParams
        };
        return Problem(
            statusCode: problem.Status,
            title: problem.Title,
            detail: problem.Detail,
            instance: HttpContext.Request.Path,
            extensions: problem.Extensions
        );
    }

    protected ActionResult UnauthorizedProblem(string? detail = null, string? requiredAuth = null,
        IEnumerable<string>? missingClaims = null) {
        var problem = new UnauthorizedProblemDetails {
            Status = StatusCodes.Status401Unauthorized,
            Title = "Unauthorized",
            Detail = detail ?? "Authentication is required and has failed or has not yet been provided.",
            RequiredAuth = requiredAuth,
            MissingClaims = missingClaims
        };
        return Problem(
            statusCode: problem.Status,
            title: problem.Title,
            detail: problem.Detail,
            instance: HttpContext.Request.Path,
            extensions: problem.Extensions
        );
    }

    protected ActionResult ForbiddenProblem(string? detail = null, string? requiredRole = null,
        string? requiredPermission = null) {
        var problem = new ForbiddenProblemDetails {
            Status = StatusCodes.Status403Forbidden,
            Title = "Forbidden",
            Detail = detail ?? "You do not have permission to access this resource.",
            RequiredRole = requiredRole,
            RequiredPermission = requiredPermission
        };
        return Problem(
            statusCode: problem.Status,
            title: problem.Title,
            detail: problem.Detail,
            instance: HttpContext.Request.Path,
            extensions: problem.Extensions
        );
    }
}
