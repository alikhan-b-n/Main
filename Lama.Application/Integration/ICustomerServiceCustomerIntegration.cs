namespace Lama.Application.Integration;

/// <summary>
/// Integration service for Customer Service -> Customer Management
/// Support interactions feed back to enrich customer data
/// </summary>
public interface ICustomerServiceCustomerIntegration
{
    /// <summary>
    /// Update customer profile based on support interactions
    /// </summary>
    Task UpdateCustomerProfileFromInteractionsAsync(Guid accountId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Add notes to customer account from support case resolution
    /// </summary>
    Task AddCustomerNoteFromCaseAsync(Guid accountId, Guid supportCaseId, string note, CancellationToken cancellationToken = default);

    /// <summary>
    /// Track customer sentiment from support interactions
    /// </summary>
    Task<CustomerSentimentDto> GetCustomerSentimentAsync(Guid accountId, CancellationToken cancellationToken = default);
}

public record CustomerSentimentDto(
    Guid AccountId,
    string SentimentScore,
    int PositiveInteractions,
    int NegativeInteractions,
    int NeutralInteractions,
    List<string> RecentFeedback
);
