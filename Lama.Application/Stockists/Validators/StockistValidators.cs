using FluentValidation;
using Lama.Application.Stockists.Commands;
using Lama.Domain.Stockists;
using Lama.Domain.Stockists.Entities;

namespace Lama.Application.Stockists.Validators;

public class StockistDetailsValidator : AbstractValidator<StockistDetails>
{
    public StockistDetailsValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Store name is required")
            .MaximumLength(Stockist.MaxNameLength);
        RuleFor(x => x.Region).MaximumLength(Stockist.MaxRegionLength);
        RuleFor(x => x.Address).MaximumLength(Stockist.MaxAddressLength);
        RuleFor(x => x.ContactName).MaximumLength(Stockist.MaxContactNameLength);
        RuleFor(x => x.Phone).MaximumLength(Stockist.MaxPhoneLength);
        RuleFor(x => x.Email)
            .MaximumLength(Stockist.MaxEmailLength)
            .EmailAddress().WithMessage("Email must be a valid email address")
            .When(x => !string.IsNullOrWhiteSpace(x.Email));
    }
}

public class CreateStockistCommandValidator : AbstractValidator<CreateStockistCommand>
{
    public CreateStockistCommandValidator()
    {
        RuleFor(x => x.Details).NotNull().SetValidator(new StockistDetailsValidator());
        RuleFor(x => x.Stage).IsInEnum();
        RuleFor(x => x.Note).MaximumLength(StockistEvent.MaxTextLength);
    }
}

public class UpdateStockistCommandValidator : AbstractValidator<UpdateStockistCommand>
{
    public UpdateStockistCommandValidator()
    {
        RuleFor(x => x.Details).NotNull().SetValidator(new StockistDetailsValidator());
    }
}

public class ChangeStockistStageCommandValidator : AbstractValidator<ChangeStockistStageCommand>
{
    public ChangeStockistStageCommandValidator()
    {
        RuleFor(x => x.Stage).IsInEnum();
    }
}

public class RecordStockCommandValidator : AbstractValidator<RecordStockCommand>
{
    public RecordStockCommandValidator()
    {
        RuleFor(x => x.Movement).IsInEnum();
        RuleFor(x => x.Title)
            .Must(t => BookTitles.IsKnown(t?.Trim().ToLowerInvariant()))
            .WithMessage($"Title must be one of: {string.Join(", ", BookTitles.Codes)}");
        // Count and Restock say what is on the shelf, which can be none
        RuleFor(x => x.Quantity)
            .GreaterThan(0).When(x => x.Movement is StockMovement.Delivery or StockMovement.Return)
            .WithMessage("Quantity must be at least 1");
        RuleFor(x => x.Quantity)
            .GreaterThanOrEqualTo(0).When(x => x.Movement is StockMovement.Count or StockMovement.Restock)
            .WithMessage("Quantity cannot be negative");
        RuleFor(x => x.Quantity).LessThanOrEqualTo(10_000);
        RuleFor(x => x.Added).InclusiveBetween(0, 10_000);
        // Shipping is paid when books go out: a delivery, or a restock that brings some in
        RuleFor(x => x.ShippingCost)
            .Must((x, cost) => cost is null or 0 || x.Movement == StockMovement.Delivery
                || (x.Movement == StockMovement.Restock && x.Added > 0))
            .WithMessage("Shipping can only be added when books are sent to the store");
        RuleFor(x => x.ShippingCost).InclusiveBetween(0, 10_000).When(x => x.ShippingCost != null)
            .WithMessage("Shipping cost must be between 0 and 10,000");
        RuleFor(x => x.Note).MaximumLength(StockistEvent.MaxTextLength);
    }
}

public class AddStockistNoteCommandValidator : AbstractValidator<AddStockistNoteCommand>
{
    public AddStockistNoteCommandValidator()
    {
        RuleFor(x => x.Text)
            .NotEmpty().WithMessage("Note cannot be empty")
            .MaximumLength(StockistEvent.MaxTextLength);
    }
}

public class AddStockistInvoiceCommandValidator : AbstractValidator<AddStockistInvoiceCommand>
{
    public AddStockistInvoiceCommandValidator()
    {
        RuleFor(x => x.Amount).GreaterThanOrEqualTo(0).When(x => x.Amount.HasValue);
        RuleFor(x => x.Reference).MaximumLength(StockistInvoice.MaxReferenceLength);
    }
}

public class UpdateStockistInvoiceCommandValidator : AbstractValidator<UpdateStockistInvoiceCommand>
{
    public UpdateStockistInvoiceCommandValidator()
    {
        RuleFor(x => x.Amount).GreaterThanOrEqualTo(0).When(x => x.Amount.HasValue);
        RuleFor(x => x.Reference).MaximumLength(StockistInvoice.MaxReferenceLength);
    }
}

public class ImportStockistsCommandValidator : AbstractValidator<ImportStockistsCommand>
{
    public ImportStockistsCommandValidator()
    {
        RuleFor(x => x.Stockists).NotNull().Must(list => list.Count <= 2000)
            .WithMessage("No more than 2000 stores in one import");

        RuleForEach(x => x.Stockists).ChildRules(s =>
        {
            s.RuleFor(v => new StockistDetails(v.Name, v.Region, v.Address, v.ContactName, v.Email, v.Phone))
                .SetValidator(new StockistDetailsValidator())
                .OverridePropertyName("Details");
            s.RuleFor(v => v.Stage).IsInEnum();
            s.RuleForEach(v => v.Stock).ChildRules(stock =>
            {
                stock.RuleFor(l => l.Title)
                    .Must(t => BookTitles.IsKnown(t?.Trim().ToLowerInvariant()))
                    .WithMessage($"Title must be one of: {string.Join(", ", BookTitles.Codes)}");
                stock.RuleFor(l => l.Quantity).InclusiveBetween(0, 10_000);
            });
            s.RuleForEach(v => v.Notes).MaximumLength(StockistEvent.MaxTextLength);
            s.RuleForEach(v => v.Invoices).ChildRules(invoice =>
            {
                invoice.RuleFor(i => i.Amount).GreaterThanOrEqualTo(0).When(i => i.Amount.HasValue);
                invoice.RuleFor(i => i.Reference).MaximumLength(StockistInvoice.MaxReferenceLength);
            });
        });
    }
}
