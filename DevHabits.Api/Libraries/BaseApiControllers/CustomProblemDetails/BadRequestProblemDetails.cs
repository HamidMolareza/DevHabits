using Microsoft.AspNetCore.Mvc;

namespace DevHabits.Api.Libraries.BaseApiControllers.CustomProblemDetails;

public class BadRequestProblemDetails : ProblemDetails {
    public IDictionary<string, string[]>? InvalidParams { get; set; }
}
