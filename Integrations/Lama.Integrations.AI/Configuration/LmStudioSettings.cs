namespace Lama.Integrations.AI.Configuration;

/// <summary>
/// Default settings for LM Studio (OpenAI-compatible local server).
/// Only used as a fallback when the request does not specify
/// <c>lmstudioBaseUrl</c>.
/// </summary>
public class LmStudioSettings
{
    public const string SectionName = "LmStudio";

    public string BaseUrl { get; set; } = "http://localhost:1234";

    /// <summary>
    /// Optional default model. LM Studio uses the currently loaded model
    /// when this is empty.
    /// </summary>
    public string? Model { get; set; }

    public int TimeoutSeconds { get; set; } = 60;
}
