using FluentValidation;
using Lama.Application.SalesManagement.Commands;

namespace Lama.Application.SalesManagement.Validators;

public class CreateDealCommandValidator : AbstractValidator<CreateDealCommand>
{
    public CreateDealCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Deal name is required")
            .MaximumLength(200).WithMessage("Deal name must not exceed 200 characters");

        RuleFor(x => x.CompanyId)
            .NotEmpty().WithMessage("Company is required");

        RuleFor(x => x.PipelineId)
            .NotEmpty().WithMessage("Pipeline is required");

        RuleFor(x => x.StageId)
            .NotEmpty().WithMessage("Stage is required");

        RuleFor(x => x.Amount)
            .GreaterThanOrEqualTo(0).WithMessage("Amount must be non-negative");

        RuleFor(x => x.ExpectedCloseDate)
            .GreaterThan(DateTime.UtcNow).WithMessage("Expected close date must be in the future");

        RuleFor(x => x.Currency)
            .NotEmpty().WithMessage("Currency is required")
            .Length(3).WithMessage("Currency must be 3 characters (ISO 4217)");
    }
}
