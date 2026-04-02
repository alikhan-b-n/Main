using Lama.Application.Common;
using Lama.Domain.CustomerService.Entities;

namespace Lama.Application.TicketManagement.Queries;

public record GetAllTicketsQuery : IQuery<IEnumerable<SupportCaseDto>>;

public class GetAllTicketsQueryHandler : IQueryHandler<GetAllTicketsQuery, IEnumerable<SupportCaseDto>>
{
    private readonly IRepository<Ticket> _ticketRepository;

    public GetAllTicketsQueryHandler(IRepository<Ticket> ticketRepository)
    {
        _ticketRepository = ticketRepository;
    }

    public async Task<IEnumerable<SupportCaseDto>> Handle(GetAllTicketsQuery query, CancellationToken cancellationToken)
    {
        var tickets = await _ticketRepository.GetAllAsync(cancellationToken);

        return tickets.Select(t => new SupportCaseDto(
            t.Id,
            t.TicketName,
            t.Description,
            MapPriority(t.Priority),
            MapStatus(t.Status),
            t.CompanyId,
            t.ContactId,
            t.CreatedAt
        ));
    }

    private static string MapPriority(TicketPriority priority) => priority switch
    {
        TicketPriority.Urgent => "Critical",
        _ => priority.ToString()
    };

    private static string MapStatus(TicketStatus status) => status switch
    {
        TicketStatus.Waiting => "InProgress",
        TicketStatus.Cancelled => "Closed",
        _ => status.ToString()
    };
}

public record SupportCaseDto(
    Guid Id,
    string Title,
    string Description,
    string Priority,
    string Status,
    Guid? AccountId,
    Guid ContactId,
    DateTime CreatedAt
);
