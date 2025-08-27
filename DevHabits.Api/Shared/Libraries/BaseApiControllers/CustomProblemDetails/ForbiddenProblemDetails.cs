using Microsoft.AspNetCore.Mvc;

namespace DevHabits.Api.Shared.Libraries.BaseApiControllers.CustomProblemDetails;

public class ForbiddenProblemDetails : ProblemDetails {
    public string? RequiredRole { get; set; }
    public string? RequiredPermission { get; set; }
}
