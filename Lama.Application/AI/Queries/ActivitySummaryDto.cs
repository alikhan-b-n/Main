namespace Lama.Application.AI.Queries;

/// <summary>
/// DTO representing the result of activity summarization.
/// This is an application-level concern, not an integration concern.
/// </summary>
public record ActivitySummaryDto(Guid Id, string Summary, string AiMetadata);