using Lama.Application.Common;
using Lama.Domain.CustomerService.Entities;

namespace Lama.Application.TicketManagement.Commands;

public record CreateTicketCommand(
    string TicketName,
    string Description,
    Guid ContactId,
    TicketPriority Priority,
    TicketSource Source,
    Guid? CompanyId = null,
    Guid? OwnerId = null
) : ICommand<Guid>;

public class CreateTicketCommandHandler : ICommandHandler<CreateTicketCommand, Guid>
{
    private readonly IRepository<Ticket> _ticketRepository;
    private readonly ITicketPipelineResolver _pipelineResolver;

    public CreateTicketCommandHandler(IRepository<Ticket> ticketRepository, ITicketPipelineResolver pipelineResolver)
    {
        _ticketRepository = ticketRepository;
        _pipelineResolver = pipelineResolver;
    }

    public async Task<Guid> Handle(CreateTicketCommand command, CancellationToken cancellationToken = default)
    {
        var (pipelineId, stageId) = await _pipelineResolver.ResolveDefaultAsync(cancellationToken);

        var ticket = Ticket.Create(
            command.TicketName,
            command.Description,
            command.ContactId,
            command.Priority,
            command.Source,
            pipelineId,
            stageId
        );

        if (command.CompanyId.HasValue)
            ticket.AssignToCompany(command.CompanyId.Value);

        if (command.OwnerId.HasValue)
            ticket.AssignToOwner(command.OwnerId.Value);

        await _ticketRepository.AddAsync(ticket, cancellationToken);

        return ticket.Id;
    }
}
