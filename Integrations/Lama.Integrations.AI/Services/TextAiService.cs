using Lama.Integrations.AI.Configuration;
using Lama.Integrations.AI.Exceptions;
using Lama.Integrations.AI.Interfaces;
using Microsoft.Extensions.Logging;

namespace Lama.Integrations.AI.Services;

/// <summary>
/// Provider-agnostic text AI service. Each scenario method builds a
/// (systemPrompt, userMessage) pair and routes it through the
/// <see cref="IAiCompletionClientFactory"/>. The hardcoded default system
/// prompt is replaced with <see cref="AiProviderOptions.CustomPrompt"/>
/// when one is supplied.
///
/// Provider HTTP failures (<see cref="AiProviderException"/>) and
/// configuration errors (<see cref="AiProviderConfigurationException"/>)
/// propagate to the controller layer, which translates them into 502 and
/// 400 responses respectively. The local fallback paths are only used
/// when the provider returns a successful but empty response, preserving
/// minimal graceful-degradation for that edge case.
/// </summary>
public class TextAiService : ITextAiService
{
    private readonly IAiCompletionClientFactory _clientFactory;
    private readonly ILogger<TextAiService> _logger;

    public TextAiService(
        IAiCompletionClientFactory clientFactory,
        ILogger<TextAiService> logger)
    {
        _clientFactory = clientFactory;
        _logger = logger;
    }

    public async Task<string> SummarizeSupportCaseAsync(
        string title,
        string description,
        AiProviderOptions? providerOptions = null,
        CancellationToken cancellationToken = default)
    {
        const string defaultSystemPrompt =
            "You are a CRM assistant. Summarize the following customer support case in one short sentence (max 20 words). " +
            "Be direct and factual. Do not start with \"The customer\" or \"This case\". Just state the core issue.";

        var systemPrompt = ResolveSystemPrompt(providerOptions, defaultSystemPrompt);
        var userMessage = $"Title: {title}\nDescription: {description}\n\nSummary:";

        var result = await TryCompleteAsync(providerOptions, systemPrompt, userMessage, cancellationToken);
        return result ?? FallbackSummary(description);
    }

    public async Task<string> GenerateDashboardInsightAsync(
        DashboardContext ctx,
        AiProviderOptions? providerOptions = null,
        CancellationToken cancellationToken = default)
    {
        static string Trend(int t) => t >= 0 ? $"+{t}%" : $"{t}%";

        const string defaultSystemPrompt =
            "You are a CRM business analyst. Given the metrics provided, write exactly 2 sentences of natural-language " +
            "insight for a sales manager. Highlight what is most notable — either a positive trend or a concern. Be direct. " +
            "No bullet points, no headings.";

        var systemPrompt = ResolveSystemPrompt(providerOptions, defaultSystemPrompt);
        var userMessage = $"""
            Metrics:
            - Accounts: {ctx.TotalAccounts} ({Trend(ctx.TrendAccounts)} vs last month)
            - Contacts: {ctx.TotalContacts} ({Trend(ctx.TrendContacts)} vs last month)
            - Opportunities: {ctx.TotalOpportunities} ({Trend(ctx.TrendOpportunities)} vs last month)
            - Open support cases: {ctx.OpenCases} ({Trend(ctx.TrendCases)} vs last month)
            - Active pipeline value: ${ctx.PipelineValue:N0}
            - Deals realized this month: {ctx.WonDealsThisMonth}
            - Conversion rate: {ctx.ConversionRate}%

            Insight:
            """;

        var result = await TryCompleteAsync(providerOptions, systemPrompt, userMessage, cancellationToken);
        return result ?? FallbackInsight(ctx);
    }

    public async Task<string> SuggestCasePriorityAsync(
        string title,
        string description,
        AiProviderOptions? providerOptions = null,
        CancellationToken cancellationToken = default)
    {
        const string defaultSystemPrompt =
            "You are a CRM support triage assistant. Based on the support case below, respond with exactly one word — " +
            "the priority level. Choose only from: Low, Medium, High, Critical.\n" +
            "Rules:\n" +
            "- Critical: system down, data loss, security breach, business completely blocked\n" +
            "- High: major feature broken, significant business impact, no workaround\n" +
            "- Medium: partial issue, workaround exists, moderate impact\n" +
            "- Low: question, minor inconvenience, cosmetic issue, feature request";

        var systemPrompt = ResolveSystemPrompt(providerOptions, defaultSystemPrompt);
        var userMessage = $"Title: {title}\nDescription: {description}\n\nPriority (one word only):";

        var result = await TryCompleteAsync(providerOptions, systemPrompt, userMessage, cancellationToken);
        return NormalizePriority(result);
    }

    public async Task<string> GenerateAccountHealthAsync(
        AccountHealthContext ctx,
        AiProviderOptions? providerOptions = null,
        CancellationToken cancellationToken = default)
    {
        var lastContact = ctx.DaysSinceLastContact.HasValue
            ? ctx.DaysSinceLastContact == 0 ? "today" : $"{ctx.DaysSinceLastContact} day{(ctx.DaysSinceLastContact == 1 ? "" : "s")} ago"
            : "unknown";

        const string defaultSystemPrompt =
            "You are a CRM analyst. Write a single short sentence (max 25 words) summarizing the health of this account. " +
            "Be factual. Mention the most important signals — critical cases are urgent, no open cases is positive. " +
            "Do not start with the account name.";

        var systemPrompt = ResolveSystemPrompt(providerOptions, defaultSystemPrompt);
        var userMessage = $"""
            Account: {ctx.AccountName} ({ctx.Industry ?? "no industry"})
            Contacts on record: {ctx.TotalContacts}
            Open support cases: {ctx.OpenCases} ({ctx.CriticalCases} critical)
            Relevant opportunities: {ctx.RelevantOpportunities}
            Last contact added: {lastContact}

            Health summary:
            """;

        var result = await TryCompleteAsync(providerOptions, systemPrompt, userMessage, cancellationToken);
        return result ?? FallbackHealth(ctx);
    }

    private async Task<string?> TryCompleteAsync(
        AiProviderOptions? providerOptions,
        string systemPrompt,
        string userMessage,
        CancellationToken cancellationToken)
    {
        var options = providerOptions ?? new AiProviderOptions();
        var client = _clientFactory.GetClient(options);
        var result = await client.CompleteAsync(options, systemPrompt, userMessage, cancellationToken);
        return string.IsNullOrWhiteSpace(result) ? null : result;
    }

    private static string ResolveSystemPrompt(AiProviderOptions? providerOptions, string defaultPrompt)
    {
        if (providerOptions is null) return defaultPrompt;
        return string.IsNullOrWhiteSpace(providerOptions.CustomPrompt)
            ? defaultPrompt
            : providerOptions.CustomPrompt!;
    }

    private static string NormalizePriority(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return "Medium";
        var cleaned = raw.Trim().Split(' ', '\n')[0].ToLowerInvariant().Trim('.');
        return cleaned switch
        {
            "critical" or "urgent" => "Critical",
            "high"                 => "High",
            "low"                  => "Low",
            _                      => "Medium",
        };
    }

    private static string FallbackSummary(string description)
    {
        var first = description.Split(new[] { '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim())
            .FirstOrDefault(s => s.Length > 0);

        if (first != null)
            return first.Length <= 120 ? first : first[..117] + "...";

        return description.Length <= 120 ? description : description[..117] + "...";
    }

    private static string FallbackInsight(DashboardContext ctx)
    {
        var parts = new List<string>();

        if (ctx.OpenCases > 5)
            parts.Add($"{ctx.OpenCases} open support cases require attention.");
        else if (ctx.OpenCases == 0)
            parts.Add("No open support cases — customer service is on track.");

        if (ctx.WonDealsThisMonth > 0)
            parts.Add($"{ctx.WonDealsThisMonth} deal{(ctx.WonDealsThisMonth > 1 ? "s" : "")} realized this month with a {ctx.ConversionRate}% conversion rate.");

        if (ctx.PipelineValue > 0)
            parts.Add($"Active pipeline stands at ${ctx.PipelineValue:N0}.");

        return parts.Count > 0
            ? string.Join(" ", parts)
            : $"Tracking {ctx.TotalAccounts} accounts, {ctx.TotalOpportunities} opportunities, and {ctx.OpenCases} open cases.";
    }

    private static string FallbackHealth(AccountHealthContext ctx)
    {
        var parts = new List<string>();

        if (ctx.CriticalCases > 0)
            parts.Add($"{ctx.CriticalCases} critical case{(ctx.CriticalCases > 1 ? "s" : "")} open.");
        else if (ctx.OpenCases == 0)
            parts.Add("No open support cases.");
        else
            parts.Add($"{ctx.OpenCases} open case{(ctx.OpenCases > 1 ? "s" : "")}.");

        if (ctx.RelevantOpportunities > 0)
            parts.Add($"{ctx.RelevantOpportunities} relevant opportunit{(ctx.RelevantOpportunities > 1 ? "ies" : "y")}.");

        if (ctx.DaysSinceLastContact.HasValue)
            parts.Add($"Last contact {ctx.DaysSinceLastContact} day{(ctx.DaysSinceLastContact == 1 ? "" : "s")} ago.");

        return parts.Count > 0 ? string.Join(" ", parts) : $"{ctx.TotalContacts} contact{(ctx.TotalContacts != 1 ? "s" : "")} on record.";
    }
}
