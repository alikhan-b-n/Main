using System.Text.Json.Serialization;

namespace Lama.Api.AiRequests;

/// <summary>
/// All provider configs + which providers to include in the run.
/// </summary>
public class BenchmarkRequest
{
    // ── Which providers to test ───────────────────────────────────
    [JsonPropertyName("testOllama")]   public bool TestOllama   { get; set; }
    [JsonPropertyName("testLmStudio")] public bool TestLmStudio { get; set; }
    [JsonPropertyName("testGroq")]     public bool TestGroq     { get; set; }

    // ── Ollama ────────────────────────────────────────────────────
    [JsonPropertyName("ollamaBaseUrl")] public string? OllamaBaseUrl { get; set; }
    [JsonPropertyName("ollamaModel")]   public string? OllamaModel   { get; set; }

    // ── LM Studio ─────────────────────────────────────────────────
    [JsonPropertyName("lmstudioBaseUrl")] public string? LmStudioBaseUrl { get; set; }
    [JsonPropertyName("lmstudioModel")]   public string? LmStudioModel   { get; set; }

    // ── Groq ──────────────────────────────────────────────────────
    [JsonPropertyName("groqApiKey")] public string? GroqApiKey { get; set; }
    [JsonPropertyName("groqModel")]  public string? GroqModel  { get; set; }
}

public class BenchmarkScenarioResult
{
    public string Provider  { get; set; } = "";
    public string Scenario  { get; set; } = "";
    public bool   Success   { get; set; }
    public long   LatencyMs { get; set; }
    public int    ResponseLength { get; set; }
    public string? ResponsePreview { get; set; }
    public string? Error    { get; set; }
}

public class BenchmarkResponse
{
    public List<BenchmarkScenarioResult> Results { get; set; } = [];
    public DateTime RunAt { get; set; } = DateTime.UtcNow;
}
