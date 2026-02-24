using Lama.Application.Common;
using Lama.Domain.CustomerService.Entities;

namespace Lama.Application.TicketManagement.Queries;

public record GetAllTicketsQuery : IQuery<IEnumerable<TicketDto>>;

public class GetAllTicketsQueryHandler : IQueryHandler<GetAllTicketsQuery, IEnumerable<TicketDto>>
{
    private readonly IRepository<Ticket> _ticketRepository;

    public GetAllTicketsQueryHandler(IRepository<Ticket> ticketRepository)
    {
        _ticketRepository = ticketRepository;
    }

    public async Task<IEnumerable<TicketDto>> Handle(GetAllTicketsQuery query, CancellationToken cancellationToken)
    {
        var tickets = await _ticketRepository.GetAllAsync(cancellationToken);

        return tickets.Select(t => new TicketDto(
            t.Id,
            t.TicketName,
            t.Description,
            t.Status.ToString(),
            t.Priority.ToString(),
            t.Source.ToString(),
            t.ContactId,
            t.CompanyId,
            t.PipelineId,
            t.StageId,
            t.OwnerId,
            t.CreatedAt,
            t.ClosedAt
        ));
    }
}

public record TicketDto(
    Guid Id,
    string TicketName,
    string Description,
    string Status,
    string Priority,
    string Source,
    Guid ContactId,
    Guid? CompanyId,
    Guid PipelineId,
    Guid StageId,
    Guid? OwnerId,
    DateTime CreatedAt,
    DateTime? ClosedAt
);
