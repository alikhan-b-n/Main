using Lama.Application.Common;
using Lama.Domain.SalesManagement.Entities;

namespace Lama.Application.SalesManagement.Queries;

public record GetAllDealsQuery : IQuery<IEnumerable<DealDto>>;

public class GetAllDealsQueryHandler : IQueryHandler<GetAllDealsQuery, IEnumerable<DealDto>>
{
    private readonly IRepository<Deal> _dealRepository;

    public GetAllDealsQueryHandler(IRepository<Deal> dealRepository)
    {
        _dealRepository = dealRepository;
    }

    public async Task<IEnumerable<DealDto>> Handle(GetAllDealsQuery query, CancellationToken cancellationToken)
    {
        var deals = await _dealRepository.GetAllAsync(cancellationToken);

        return deals.Select(d => new DealDto(
            d.Id,
            d.Name,
            d.Description,
            d.CompanyId,
            d.ContactId,
            d.Amount.Amount,
            d.Amount.Currency,
            d.Probability,
            d.ExpectedCloseDate,
            d.ActualCloseDate,
            d.PipelineId,
            d.StageId,
            d.OwnerId,
            d.CreatedAt
        ));
    }
}

public record DealDto(
    Guid Id,
    string Name,
    string? Description,
    Guid CompanyId,
    Guid? ContactId,
    decimal Amount,
    string Currency,
    int Probability,
    DateTime ExpectedCloseDate,
    DateTime? ActualCloseDate,
    Guid PipelineId,
    Guid StageId,
    Guid? OwnerId,
    DateTime CreatedAt
);
