using Microsoft.AspNetCore.Mvc;

namespace DevHabits.Api.Shared.Libraries.BaseApiControllers.CustomProblemDetails;

public class UnauthorizedProblemDetails : ProblemDetails {
    public string? RequiredAuth { get; set; }
    public IEnumerable<string>? MissingClaims { get; set; }
}
