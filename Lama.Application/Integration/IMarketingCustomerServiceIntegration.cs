namespace Lama.Application.Integration;

/// <summary>
/// Integration service for Marketing Management -> Customer Service
/// Marketing campaigns trigger support outreach
/// </summary>
public interface IMarketingCustomerServiceIntegration
{
    /// <summary>
    /// Create support outreach tasks from campaign responses
    /// </summary>
    Task CreateSupportOutreachTasksAsync(Guid campaignId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Schedule follow-up for campaign respondents
    /// </summary>
    Task ScheduleFollowUpAsync(Guid campaignId, Guid contactId, DateTime scheduledDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Track campaign-generated support cases
    /// </summary>
    Task<IEnumerable<CampaignSupportCaseDto>> GetCampaignSupportCasesAsync(Guid campaignId, CancellationToken cancellationToken = default);
}

public record CampaignSupportCaseDto(
    Guid CaseId,
    Guid CampaignId,
    Guid ContactId,
    string CaseTitle,
    string CaseType,
    DateTime CreatedAt
);
