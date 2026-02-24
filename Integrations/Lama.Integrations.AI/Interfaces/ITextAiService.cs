namespace Lama.Integrations.AI.Interfaces;

public interface ITextAiService
{
    /// <summary>
    /// Generate a short summary for the provided text (subject + body).
    /// Implementations may call external providers (OpenAI, Azure) or local summarizers.
    /// </summary>
    /// <param name="subject">Activity subject (may be null/empty)</param>
    /// <param name="body">Activity body (may be null/empty)</param>
    /// <returns>Summary text</returns>
    Task<string> SummarizeAsync(string? subject, string? body, CancellationToken cancellationToken = default);
}