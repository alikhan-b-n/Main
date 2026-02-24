using Lama.Application.Common;
using Lama.Domain.CustomerService.Entities;

namespace Lama.Application.TicketManagement.Commands;

public record UpdateTicketCommand(
    Guid Id,
    string TicketName,
    string Description,
    TicketPriority Priority,
    Guid? OwnerId = null,
    Guid? StageId = null,
    Guid? CompanyId = null
) : ICommand;

public class UpdateTicketCommandHandler : ICommandHandler<UpdateTicketCommand>
{
    private readonly IRepository<Ticket> _ticketRepository;

    public UpdateTicketCommandHandler(IRepository<Ticket> ticketRepository)
    {
        _ticketRepository = ticketRepository;
    }

    public async Task Handle(UpdateTicketCommand command, CancellationToken cancellationToken = default)
    {
        var ticket = await _ticketRepository.GetByIdAsync(command.Id, cancellationToken);
        if (ticket == null)
            throw new InvalidOperationException($"Ticket with ID {command.Id} not found");

        ticket.UpdateTicketInfo(command.TicketName, command.Description, command.Priority);

        if (command.OwnerId.HasValue)
        {
            ticket.AssignToOwner(command.OwnerId.Value);
        }

        if (command.StageId.HasValue)
        {
            ticket.MoveToStage(command.StageId.Value);
        }

        if (command.CompanyId.HasValue)
        {
            ticket.AssignToCompany(command.CompanyId.Value);
        }

        await _ticketRepository.UpdateAsync(ticket, cancellationToken);
    }
}