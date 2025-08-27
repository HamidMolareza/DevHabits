using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace DevHabits.Api.Controllers;

[ApiController]
public abstract class BaseApiController : ControllerBase {
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

    #region Obsolete overrides (Error results)

    [Obsolete("Use BadRequestProblem(...) instead.")]
    [NonAction]
    public new BadRequestResult BadRequest() => base.BadRequest();

    [Obsolete("Use BadRequestProblem(...) instead.")]
    [NonAction]
    public new BadRequestObjectResult BadRequest([ActionResultObjectValue] object? error)
        => base.BadRequest(error);

    [Obsolete("Use NotFoundProblem(...) instead.")]
    [NonAction]
    public new NotFoundResult NotFound() => base.NotFound();

    [Obsolete("Use NotFoundProblem(...) instead.")]
    [NonAction]
    public new NotFoundObjectResult NotFound([ActionResultObjectValue] object? value)
        => base.NotFound(value);

    [Obsolete("Use ConflictProblem(...) instead.")]
    [NonAction]
    public new ConflictResult Conflict() => base.Conflict();

    [Obsolete("Use ConflictProblem(...) instead.")]
    [NonAction]
    public new ConflictObjectResult Conflict([ActionResultObjectValue] object? error)
        => base.Conflict(error);

    [Obsolete("Use UnauthorizedProblem(...) instead.")]
    [NonAction]
    public new UnauthorizedResult Unauthorized() => base.Unauthorized();

    [Obsolete("Use UnauthorizedProblem(...) instead.")]
    [NonAction]
    public new UnauthorizedObjectResult Unauthorized([ActionResultObjectValue] object? value)
        => base.Unauthorized(value);

    [Obsolete("Use ForbiddenProblem(...) instead.")]
    [NonAction]
    public new ForbidResult Forbid() => base.Forbid();

    #endregion
}

public class NotFoundProblemDetails : ProblemDetails {
    public string? Resource { get; set; }
    public string? ResourceId { get; set; }
}

public class ConflictProblemDetails : ProblemDetails {
    public string? ConflictWithId { get; set; }
    public string? CurrentValue { get; set; }
}

public class BadRequestProblemDetails : ProblemDetails {
    public IDictionary<string, string[]>? InvalidParams { get; set; }
}

public class UnauthorizedProblemDetails : ProblemDetails {
    public string? RequiredAuth { get; set; }
    public IEnumerable<string>? MissingClaims { get; set; }
}

public class ForbiddenProblemDetails : ProblemDetails {
    public string? RequiredRole { get; set; }
    public string? RequiredPermission { get; set; }
}
