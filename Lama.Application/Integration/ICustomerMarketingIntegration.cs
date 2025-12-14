namespace Lama.Application.Integration;

/// <summary>
/// Integration service for Customer Management -> Marketing Management
/// Provides customer targeting capabilities for marketing campaigns
/// </summary>
public interface ICustomerMarketingIntegration
{
    /// <summary>
    /// Get target customers for a campaign based on segment criteria
    /// </summary>
    Task<IEnumerable<TargetCustomerDto>> GetTargetCustomersForCampaignAsync(Guid segmentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get accounts matching marketing criteria
    /// </summary>
    Task<IEnumerable<Guid>> GetAccountsByIndustryAsync(string industry, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get contacts for email campaigns
    /// </summary>
    Task<IEnumerable<ContactTargetDto>> GetContactsForEmailCampaignAsync(Guid campaignId, CancellationToken cancellationToken = default);
}

public record TargetCustomerDto(Guid AccountId, string Name, string? Industry, string? Email);
public record ContactTargetDto(Guid ContactId, string FullName, string Email, Guid? AccountId);
