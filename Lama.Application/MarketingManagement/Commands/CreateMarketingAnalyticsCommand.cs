using Lama.Application.Common;
using Lama.Domain.MarketingManagement.Entities;

namespace Lama.Application.MarketingManagement.Commands;

public record CreateMarketingAnalyticsCommand(
    string Name,
    AnalyticsType Type,
    DateTime PeriodStart,
    DateTime PeriodEnd,
    Guid? CampaignId = null
) : ICommand<Guid>;

public class CreateMarketingAnalyticsCommandHandler : ICommandHandler<CreateMarketingAnalyticsCommand, Guid>
{
    private readonly IRepository<MarketingAnalytics> _analyticsRepository;

    public CreateMarketingAnalyticsCommandHandler(IRepository<MarketingAnalytics> analyticsRepository)
    {
        _analyticsRepository = analyticsRepository;
    }

    public async Task<Guid> Handle(CreateMarketingAnalyticsCommand command, CancellationToken cancellationToken = default)
    {
        var analytics = MarketingAnalytics.Create(
            command.Name,
            command.Type,
            command.PeriodStart,
            command.PeriodEnd,
            command.CampaignId
        );

        await _analyticsRepository.AddAsync(analytics, cancellationToken);

        return analytics.Id;
    }
}
