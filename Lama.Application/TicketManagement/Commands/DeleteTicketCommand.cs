using Lama.Application.Common;
using Lama.Domain.CustomerService.Entities;

namespace Lama.Application.TicketManagement.Commands;

public record DeleteTicketCommand(Guid Id) : ICommand;

public class DeleteTicketCommandHandler : ICommandHandler<DeleteTicketCommand>
{
    private readonly IRepository<Ticket> _ticketRepository;

    public DeleteTicketCommandHandler(IRepository<Ticket> ticketRepository)
    {
        _ticketRepository = ticketRepository;
    }

    public async Task Handle(DeleteTicketCommand command, CancellationToken cancellationToken = default)
    {
        var ticket = await _ticketRepository.GetByIdAsync(command.Id, cancellationToken);
        if (ticket == null)
            throw new InvalidOperationException($"Ticket with ID {command.Id} not found");

        await _ticketRepository.DeleteAsync(ticket, cancellationToken);
    }
}