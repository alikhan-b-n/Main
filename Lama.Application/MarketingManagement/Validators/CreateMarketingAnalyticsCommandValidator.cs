using FluentValidation;
using Lama.Application.MarketingManagement.Commands;

namespace Lama.Application.MarketingManagement.Validators;

public class CreateMarketingAnalyticsCommandValidator : AbstractValidator<CreateMarketingAnalyticsCommand>
{
    public CreateMarketingAnalyticsCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Analytics name is required")
            .MaximumLength(200).WithMessage("Analytics name must not exceed 200 characters");

        RuleFor(x => x.Type)
            .IsInEnum().WithMessage("Invalid analytics type");

        RuleFor(x => x.PeriodStart)
            .NotEmpty().WithMessage("Period start date is required");

        RuleFor(x => x.PeriodEnd)
            .NotEmpty().WithMessage("Period end date is required")
            .GreaterThan(x => x.PeriodStart).WithMessage("Period end date must be after period start date");
    }
}