using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace DevHabits.Api.Shared.Libraries.BaseApiControllers;

public abstract partial class BaseApiController {
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

