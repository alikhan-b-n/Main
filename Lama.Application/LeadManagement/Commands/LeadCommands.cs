using Lama.Application.Common;
using Lama.Domain.LeadManagement.Entities;

namespace Lama.Application.LeadManagement.Commands;

public record ChangeLeadStatusCommand(Guid Id, LeadStatus Status, string? LostReason = null) : ICommand;

public class ChangeLeadStatusCommandHandler : ICommandHandler<ChangeLeadStatusCommand>
{
    private readonly ILeadRepository _leads;

    public ChangeLeadStatusCommandHandler(ILeadRepository leads)
    {
        _leads = leads;
    }

    public async Task Handle(ChangeLeadStatusCommand command, CancellationToken cancellationToken)
    {
        var lead = await _leads.GetByIdAsync(command.Id, cancellationToken)
                   ?? throw new LeadNotFoundException(command.Id);

        lead.ChangeStatus(command.Status, command.LostReason);
        await _leads.SaveChangesAsync(cancellationToken);
    }
}

public record AddLeadNoteCommand(Guid Id, string Text) : ICommand;

public class AddLeadNoteCommandHandler : ICommandHandler<AddLeadNoteCommand>
{
    private readonly ILeadRepository _leads;

    public AddLeadNoteCommandHandler(ILeadRepository leads)
    {
        _leads = leads;
    }

    public async Task Handle(AddLeadNoteCommand command, CancellationToken cancellationToken)
    {
        var lead = await _leads.GetByIdAsync(command.Id, cancellationToken)
                   ?? throw new LeadNotFoundException(command.Id);

        lead.AddNote(command.Text);
        await _leads.SaveChangesAsync(cancellationToken);
    }
}

public record DeleteLeadCommand(Guid Id) : ICommand;

public class DeleteLeadCommandHandler : ICommandHandler<DeleteLeadCommand>
{
    private readonly ILeadRepository _leads;

    public DeleteLeadCommandHandler(ILeadRepository leads)
    {
        _leads = leads;
    }

    public async Task Handle(DeleteLeadCommand command, CancellationToken cancellationToken)
    {
        var lead = await _leads.GetByIdAsync(command.Id, cancellationToken)
                   ?? throw new LeadNotFoundException(command.Id);

        await _leads.DeleteAsync(lead, cancellationToken);
    }
}
