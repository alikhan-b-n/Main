using Lama.Application.Common;
using Lama.Domain.CustomerService.Entities;

namespace Lama.Application.TicketManagement.Commands;

public record CreateTicketCommand(
    string TicketName,
    string Description,
    TicketPriority Priority,
    TicketSource Source,
    Guid? ContactId = null,
    Guid? CompanyId = null,
    Guid? OwnerId = null
) : ICommand<Guid>;

public class CreateTicketCommandHandler : ICommandHandler<CreateTicketCommand, Guid>
{
    private readonly IRepository<Ticket> _ticketRepository;

    public CreateTicketCommandHandler(IRepository<Ticket> ticketRepository)
    {
        _ticketRepository = ticketRepository;
    }

    public async Task<Guid> Handle(CreateTicketCommand command, CancellationToken cancellationToken = default)
    {
        var ticket = Ticket.Create(
            command.TicketName,
            command.Description,
            command.Priority,
            command.Source,
            command.ContactId
        );

        if (command.CompanyId.HasValue)
            ticket.AssignToCompany(command.CompanyId.Value);

        if (command.OwnerId.HasValue)
            ticket.AssignToOwner(command.OwnerId.Value);

        await _ticketRepository.AddAsync(ticket, cancellationToken);

        return ticket.Id;
    }
}
