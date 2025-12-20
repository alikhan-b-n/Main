using FluentValidation;
using Lama.Application.SalesManagement.Commands;

namespace Lama.Application.SalesManagement.Validators;

public class CreateOpportunityCommandValidator : AbstractValidator<CreateOpportunityCommand>
{
    public CreateOpportunityCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Opportunity name is required")
            .MaximumLength(200).WithMessage("Opportunity name must not exceed 200 characters");

        RuleFor(x => x.AccountId)
            .NotEmpty().WithMessage("Account ID is required");

        RuleFor(x => x.ExpectedRevenue)
            .GreaterThan(0).WithMessage("Expected revenue must be greater than zero");

        RuleFor(x => x.ExpectedCloseDate)
            .GreaterThan(DateTime.UtcNow).WithMessage("Expected close date must be in the future");

        RuleFor(x => x.Currency)
            .NotEmpty().WithMessage("Currency is required")
            .Length(3).WithMessage("Currency must be a 3-letter code (e.g., USD, EUR)");

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Description must not exceed 1000 characters")
            .When(x => !string.IsNullOrWhiteSpace(x.Description));
    }
}