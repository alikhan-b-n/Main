using Lama.Integrations.AI.Configuration;

namespace Lama.Integrations.AI.Interfaces;

public interface ITextAiService
{
    Task<string> SummarizeSupportCaseAsync(
        string title,
        string description,
        AiProviderOptions? providerOptions = null,
        CancellationToken cancellationToken = default);

    Task<string> GenerateDashboardInsightAsync(
        DashboardContext context,
        AiProviderOptions? providerOptions = null,
        CancellationToken cancellationToken = default);

    Task<string> SuggestCasePriorityAsync(
        string title,
        string description,
        AiProviderOptions? providerOptions = null,
        CancellationToken cancellationToken = default);

    Task<string> GenerateAccountHealthAsync(
        AccountHealthContext context,
        AiProviderOptions? providerOptions = null,
        CancellationToken cancellationToken = default);
}

public record AccountHealthContext(
    string AccountName,
    string? Industry,
    int TotalContacts,
    int OpenCases,
    int CriticalCases,
    int RelevantOpportunities,
    int? DaysSinceLastContact
);

public record DashboardContext(
    int TotalAccounts,
    int TotalContacts,
    int TotalOpportunities,
    int OpenCases,
    decimal PipelineValue,
    int WonDealsThisMonth,
    int ConversionRate,
    int TrendAccounts,
    int TrendContacts,
    int TrendOpportunities,
    int TrendCases
);
