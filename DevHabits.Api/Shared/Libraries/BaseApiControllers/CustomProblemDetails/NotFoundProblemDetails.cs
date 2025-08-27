using Microsoft.AspNetCore.Mvc;

namespace DevHabits.Api.Shared.Libraries.BaseApiControllers.CustomProblemDetails;

public class NotFoundProblemDetails : ProblemDetails {
    public string? Resource { get; set; }
    public string? ResourceId { get; set; }
}
