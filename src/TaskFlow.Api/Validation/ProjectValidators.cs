using FluentValidation;
using TaskFlow.Api.Contracts.Projects;

namespace TaskFlow.Api.Validation;

public sealed class ProjectRequestValidator : AbstractValidator<ProjectRequest>
{
    public ProjectRequestValidator()
    {
        RuleFor(x => x.Key)
            .NotEmpty()
            .MinimumLength(2)
            .MaximumLength(10)
            .Matches("^[A-Za-z][A-Za-z0-9]*$")
            .WithMessage("Key must start with a letter and contain only letters and digits.");

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(150);

        RuleFor(x => x.Description)
            .MaximumLength(2000);
    }
}
