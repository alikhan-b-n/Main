using Lama.Application.Common;
using Lama.Domain.CustomerService.Entities;
using Lama.Integrations.AI.Interfaces;

namespace Lama.Application.TicketManagement.Queries;

public record SummarizeSupportCaseQuery(Guid TicketId) : IQuery<string>;

public class SummarizeSupportCaseQueryHandler : IQueryHandler<SummarizeSupportCaseQuery, string>
{
    private readonly IRepository<Ticket> _ticketRepository;
    private readonly ITextAiService _textAiService;

    public SummarizeSupportCaseQueryHandler(
        IRepository<Ticket> ticketRepository,
        ITextAiService textAiService)
    {
        _ticketRepository = ticketRepository;
        _textAiService = textAiService;
    }

    public async Task<string> Handle(SummarizeSupportCaseQuery query, CancellationToken cancellationToken)
    {
        var ticket = await _ticketRepository.GetByIdAsync(query.TicketId, cancellationToken);
        if (ticket == null)
            throw new KeyNotFoundException($"Support case {query.TicketId} not found");

        var summary = await _textAiService.SummarizeAsync(ticket.TicketName, ticket.Description, cancellationToken);
        return summary;
    }
}
