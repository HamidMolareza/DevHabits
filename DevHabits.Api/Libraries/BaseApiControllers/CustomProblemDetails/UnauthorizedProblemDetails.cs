using Microsoft.AspNetCore.Mvc;

namespace DevHabits.Api.Libraries.BaseApiControllers.CustomProblemDetails;

public class UnauthorizedProblemDetails : ProblemDetails {
    public string? RequiredAuth { get; set; }
    public IEnumerable<string>? MissingClaims { get; set; }
}
