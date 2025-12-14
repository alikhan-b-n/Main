namespace Lama.Application.Integration;

/// <summary>
/// Integration service for Customer Service -> Sales Management
/// Support interactions identify upsell and cross-sell opportunities
/// </summary>
public interface ICustomerServiceSalesIntegration
{
    /// <summary>
    /// Identify upsell opportunities from support cases
    /// </summary>
    Task<IEnumerable<UpsellOpportunityDto>> IdentifyUpsellOpportunitiesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Create opportunity from support case
    /// </summary>
    Task<Guid> CreateOpportunityFromSupportCaseAsync(Guid supportCaseId, string opportunityName, decimal estimatedValue, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get customer health score to inform sales strategy
    /// </summary>
    Task<CustomerHealthScoreDto> GetCustomerHealthScoreAsync(Guid accountId, CancellationToken cancellationToken = default);
}

public record UpsellOpportunityDto(
    Guid SupportCaseId,
    Guid AccountId,
    string AccountName,
    string Reason,
    decimal EstimatedValue
);

public record CustomerHealthScoreDto(
    Guid AccountId,
    int Score,
    int OpenCases,
    int ResolvedCases,
    decimal AverageResolutionTime,
    string HealthStatus
);
