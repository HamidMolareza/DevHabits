using FluentValidation;

namespace DevHabits.Api.Tags.Dtos;

public sealed record CreateTagRequest {
    public required string Name { get; set; }
    public string? Description { get; set; }
}

public sealed class CreateTagRequestValidator : AbstractValidator<CreateTagRequest> {
    public CreateTagRequestValidator() {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MinimumLength(3)
            .MaximumLength(100);

        RuleFor(x => x.Description)
            .MaximumLength(500);
    }
}
