using FluentValidation;
using Lama.Application.TicketManagement.Commands;

namespace Lama.Application.TicketManagement.Validators;

public class CreateTicketCommandValidator : AbstractValidator<CreateTicketCommand>
{
    public CreateTicketCommandValidator()
    {
        RuleFor(x => x.TicketName)
            .NotEmpty().WithMessage("Ticket name is required")
            .MaximumLength(200).WithMessage("Ticket name must not exceed 200 characters");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required")
            .MaximumLength(2000).WithMessage("Description must not exceed 2000 characters");

        RuleFor(x => x.ContactId)
            .NotEmpty().WithMessage("Contact is required");

        RuleFor(x => x.PipelineId)
            .NotEmpty().WithMessage("Pipeline is required");

        RuleFor(x => x.StageId)
            .NotEmpty().WithMessage("Stage is required");

        RuleFor(x => x.Priority)
            .IsInEnum().WithMessage("Invalid priority");

        RuleFor(x => x.Source)
            .IsInEnum().WithMessage("Invalid source");
    }
}
