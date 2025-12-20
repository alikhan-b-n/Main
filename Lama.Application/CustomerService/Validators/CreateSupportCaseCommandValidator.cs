using FluentValidation;
using Lama.Application.CustomerService.Commands;

namespace Lama.Application.CustomerService.Validators;

public class CreateSupportCaseCommandValidator : AbstractValidator<CreateSupportCaseCommand>
{
    public CreateSupportCaseCommandValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Case title is required")
            .MaximumLength(200).WithMessage("Case title must not exceed 200 characters");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Case description is required")
            .MaximumLength(2000).WithMessage("Case description must not exceed 2000 characters");

        RuleFor(x => x.ContactId)
            .NotEmpty().WithMessage("Contact ID is required");

        RuleFor(x => x.Priority)
            .IsInEnum().WithMessage("Invalid case priority");

        RuleFor(x => x.Type)
            .IsInEnum().WithMessage("Invalid case type");
    }
}