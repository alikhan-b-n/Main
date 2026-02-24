using Lama.Integrations.AI.Interfaces;

namespace Lama.Integrations.AI.Services;

public class LocalTextAiService : ITextAiService
{
    public Task<string> SummarizeAsync(string? subject, string? body, CancellationToken cancellationToken = default)
    {
        // Very simple summarization heuristic: prefer subject; if body present, return first meaningful sentence or first 200 chars.
        if (string.IsNullOrWhiteSpace(subject) && string.IsNullOrWhiteSpace(body))
            return Task.FromResult(string.Empty);

        if (!string.IsNullOrWhiteSpace(subject) && string.IsNullOrWhiteSpace(body))
            return Task.FromResult(subject.Trim());

        var combined = (subject + "\n\n" + (body ?? string.Empty)).Trim();

        // Try to extract the first sentence from the body
        var sentences = body?.Split(new[] { '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim())
            .Where(s => !string.IsNullOrWhiteSpace(s));

        if (sentences != null && sentences.Any())
        {
            var first = sentences.First();
            var result = !string.IsNullOrWhiteSpace(subject) ? subject.Trim() + " — " + first : first;
            if (result.Length <= 500) return Task.FromResult(result);
        }

        // Fallback: take first 200 characters of combined text
        var snippet = combined.Length <= 200 ? combined : combined.Substring(0, 197) + "...";
        return Task.FromResult(snippet);
    }
}