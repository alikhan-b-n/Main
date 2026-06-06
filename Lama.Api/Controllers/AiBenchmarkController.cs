using System.Diagnostics;
using Lama.Api.AiRequests;
using Lama.Integrations.AI.Configuration;
using Lama.Integrations.AI.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Lama.Api.Controllers;

/// <summary>
/// Runs a fixed set of CRM-representative prompts against every requested
/// AI provider and returns latency + response data so the frontend can
/// render a performance-comparison table.
/// </summary>
[ApiController]
[Route("api/ai")]
public class AiBenchmarkController : ControllerBase
{
    // ── Fixed test scenarios ──────────────────────────────────────
    private static readonly (string Name, string System, string User)[] Scenarios =
    [
        (
            "Suggest Priority",
            "You are a support triage assistant. Based on the case description, reply with exactly one word: Low, Medium, High, or Critical.",
            "Customer cannot log in after a password reset. The issue has persisted for 2 days and affects 50+ enterprise users. Their SLA deadline is tomorrow."
        ),
        (
            "Summarize Case",
            "You are a support assistant. Summarize the following support case in one sentence of no more than 20 words.",
            "The client contacted our support team regarding recurring synchronization failures between their mobile application and the backend servers. They mentioned that the issue started after the latest software update deployed last Tuesday and has been affecting their daily operations significantly. Multiple users within the organization are impacted and they need an urgent resolution."
        ),
        (
            "Dashboard Insight",
            "You are a CRM business analyst. Provide a 2-sentence insight about these metrics. Be concise and actionable.",
            "Open deals: 24, Won this month: 8, Lost: 3, Total pipeline: $485,000, Avg deal size: $20,208, Support cases open: 12, Critical cases: 2, Accounts at risk: 3."
        ),
        (
            "Account Health",
            "You are a CRM account health analyst. Summarize the account health in one sentence based on the data provided.",
            "Account: Acme Corp. Open support cases: 4 (1 critical). Last contact: 18 days ago. Open opportunities: 2 ($95,000). Contacts: 6. Contract renewal: 45 days."
        ),
    ];

    private readonly IAiCompletionClientFactory _factory;
    private readonly ILogger<AiBenchmarkController> _logger;

    public AiBenchmarkController(
        IAiCompletionClientFactory factory,
        ILogger<AiBenchmarkController> logger)
    {
        _factory = factory;
        _logger  = logger;
    }

    [HttpPost("benchmark")]
    public async Task<IActionResult> RunBenchmark(
        [FromBody] BenchmarkRequest req,
        CancellationToken ct)
    {
        var response = new BenchmarkResponse();

        // Build list of (providerName, options) to test
        var providers = new List<(string Name, AiProviderOptions Options)>();

        if (req.TestOllama)
            providers.Add(("ollama", new AiProviderOptions
            {
                Provider      = "ollama",
                OllamaBaseUrl = req.OllamaBaseUrl,
                OllamaModel   = req.OllamaModel,
            }));

        if (req.TestLmStudio)
            providers.Add(("lmstudio", new AiProviderOptions
            {
                Provider       = "lmstudio",
                LmStudioBaseUrl = req.LmStudioBaseUrl,
                LmStudioModel   = req.LmStudioModel,
            }));

        if (req.TestGroq)
            providers.Add(("groq", new AiProviderOptions
            {
                Provider  = "groq",
                GroqApiKey = req.GroqApiKey,
                GroqModel  = req.GroqModel,
            }));

        if (providers.Count == 0)
            return BadRequest(new { error = "Select at least one provider to benchmark." });

        // Run all scenarios × providers sequentially to avoid resource contention
        foreach (var (provName, opts) in providers)
        {
            foreach (var (scenName, systemPrompt, userMessage) in Scenarios)
            {
                var result = new BenchmarkScenarioResult
                {
                    Provider = provName,
                    Scenario = scenName,
                };

                try
                {
                    var client = _factory.GetClient(opts);
                    var sw = Stopwatch.StartNew();

                    var text = await client.CompleteAsync(opts, systemPrompt, userMessage, ct);

                    sw.Stop();
                    result.Success          = !string.IsNullOrWhiteSpace(text);
                    result.LatencyMs        = sw.ElapsedMilliseconds;
                    result.ResponseLength   = text?.Length ?? 0;
                    result.ResponsePreview  = text?.Length > 120 ? text[..120] + "…" : text;
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    result.Success  = false;
                    result.Error    = ex.Message.Length > 200 ? ex.Message[..200] : ex.Message;
                    _logger.LogWarning(ex, "Benchmark failed: provider={Provider} scenario={Scenario}",
                        provName, scenName);
                }

                response.Results.Add(result);
            }
        }

        return Ok(response);
    }
}
