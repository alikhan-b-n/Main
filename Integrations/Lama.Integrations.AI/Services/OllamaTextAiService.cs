using System.Text;
using System.Text.Json;
using Lama.Integrations.AI.Configuration;
using Lama.Integrations.AI.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lama.Integrations.AI.Services;

public class OllamaTextAiService : ITextAiService
{
    private readonly HttpClient _httpClient;
    private readonly OllamaSettings _settings;
    private readonly ILogger<OllamaTextAiService> _logger;

    public OllamaTextAiService(
        HttpClient httpClient,
        IOptions<OllamaSettings> settings,
        ILogger<OllamaTextAiService> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _httpClient.BaseAddress = new Uri(_settings.BaseUrl);
        _httpClient.Timeout = TimeSpan.FromSeconds(_settings.TimeoutSeconds);
        _logger = logger;
    }

    public async Task<string> SummarizeSupportCaseAsync(string title, string description, CancellationToken cancellationToken = default)
    {
        var prompt = $"""
            You are a CRM assistant. Summarize the following customer support case in one short sentence (max 20 words).
            Be direct and factual. Do not start with "The customer" or "This case". Just state the core issue.

            Title: {title}
            Description: {description}

            Summary:
            """;

        try
        {
            var result = await CallOllamaAsync(prompt, cancellationToken);
            return result ?? FallbackSummary(description);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Ollama unavailable, using fallback summarization");
            return FallbackSummary(description);
        }
    }

    public async Task<string> GenerateDashboardInsightAsync(DashboardContext ctx, CancellationToken cancellationToken = default)
    {
        static string Trend(int t) => t >= 0 ? $"+{t}%" : $"{t}%";

        var prompt = $"""
            You are a CRM business analyst. Given the metrics below, write exactly 2 sentences of natural-language insight for a sales manager.
            Highlight what is most notable — either a positive trend or a concern. Be direct. No bullet points, no headings.

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

        try
        {
            var result = await CallOllamaAsync(prompt, cancellationToken);
            return result ?? FallbackInsight(ctx);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Ollama unavailable for dashboard insight");
            return FallbackInsight(ctx);
        }
    }

    public async Task<string> SuggestCasePriorityAsync(string title, string description, CancellationToken cancellationToken = default)
    {
        var prompt = $"""
            You are a CRM support triage assistant. Based on the support case below, respond with exactly one word — the priority level.
            Choose only from: Low, Medium, High, Critical.

            Rules:
            - Critical: system down, data loss, security breach, business completely blocked
            - High: major feature broken, significant business impact, no workaround
            - Medium: partial issue, workaround exists, moderate impact
            - Low: question, minor inconvenience, cosmetic issue, feature request

            Title: {title}
            Description: {description}

            Priority (one word only):
            """;

        try
        {
            var result = await CallOllamaAsync(prompt, cancellationToken);
            return NormalizePriority(result);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Ollama unavailable for priority suggestion");
            return "Medium";
        }
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

    public async Task<string> GenerateAccountHealthAsync(AccountHealthContext ctx, CancellationToken cancellationToken = default)
    {
        var lastContact = ctx.DaysSinceLastContact.HasValue
            ? ctx.DaysSinceLastContact == 0 ? "today" : $"{ctx.DaysSinceLastContact} day{(ctx.DaysSinceLastContact == 1 ? "" : "s")} ago"
            : "unknown";

        var prompt = $"""
            You are a CRM analyst. Write a single short sentence (max 25 words) summarizing the health of this account.
            Be factual. Mention the most important signals — critical cases are urgent, no open cases is positive.
            Do not start with the account name.

            Account: {ctx.AccountName} ({ctx.Industry ?? "no industry"})
            Contacts on record: {ctx.TotalContacts}
            Open support cases: {ctx.OpenCases} ({ctx.CriticalCases} critical)
            Relevant opportunities: {ctx.RelevantOpportunities}
            Last contact added: {lastContact}

            Health summary:
            """;

        try
        {
            var result = await CallOllamaAsync(prompt, cancellationToken);
            return result ?? FallbackHealth(ctx);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Ollama unavailable for account health");
            return FallbackHealth(ctx);
        }
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

    private async Task<string?> CallOllamaAsync(string prompt, CancellationToken cancellationToken)
    {
        var requestBody = new
        {
            model = _settings.Model,
            prompt,
            stream = false,
            options = new
            {
                temperature = _settings.Temperature,
                num_predict = _settings.MaxTokens
            }
        };

        var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
        _logger.LogInformation("Calling Ollama with model: {Model}", _settings.Model);

        var response = await _httpClient.PostAsync("/api/generate", content, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Ollama returned {Status}", response.StatusCode);
            return null;
        }

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var result = JsonSerializer.Deserialize<OllamaResponse>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        return result?.Response?.Trim();
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

    private class OllamaResponse
    {
        public string? Response { get; set; }
    }
}
