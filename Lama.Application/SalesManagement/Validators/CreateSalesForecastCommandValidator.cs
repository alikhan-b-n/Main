using FluentValidation;
using Lama.Application.SalesManagement.Commands;

namespace Lama.Application.SalesManagement.Validators;

public class CreateSalesForecastCommandValidator : AbstractValidator<CreateSalesForecastCommand>
{
    public CreateSalesForecastCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Forecast name is required")
            .MaximumLength(200).WithMessage("Forecast name must not exceed 200 characters");

        RuleFor(x => x.Period)
            .IsInEnum().WithMessage("Invalid forecast period");

        RuleFor(x => x.StartDate)
            .NotEmpty().WithMessage("Start date is required");

        RuleFor(x => x.EndDate)
            .NotEmpty().WithMessage("End date is required")
            .GreaterThan(x => x.StartDate).WithMessage("End date must be after start date");

        RuleFor(x => x.Quota)
            .GreaterThan(0).WithMessage("Quota must be greater than zero");

        RuleFor(x => x.Currency)
            .NotEmpty().WithMessage("Currency is required")
            .Length(3).WithMessage("Currency must be a 3-letter code (e.g., USD, EUR)");
    }
}