using Microsoft.AspNetCore.Mvc;

namespace DevHabits.Api.Shared.Libraries.BaseApiControllers.CustomProblemDetails;

public class ConflictProblemDetails : ProblemDetails {
    public string? ConflictWithId { get; set; }
    public string? CurrentValue { get; set; }
}
