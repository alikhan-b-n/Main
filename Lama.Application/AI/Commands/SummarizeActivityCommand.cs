using Lama.Application.Common;
using Lama.Application.AI.Queries;
using Lama.Domain.ActivityManagement.Entities;
using Lama.Integrations.AI.Interfaces;
using MediatR;

namespace Lama.Application.AI.Commands;

/// <summary>
/// Command to summarize an activity using AI.
/// This is business logic - orchestrates the use of AI service and repository.
/// </summary>
public record SummarizeActivityCommand(Guid ActivityId) : IRequest<ActivitySummaryDto>;

public class SummarizeActivityCommandHandler : IRequestHandler<SummarizeActivityCommand, ActivitySummaryDto>
{
    private readonly IRepository<Activity> _activityRepository;
    private readonly ITextAiService _textAiService;

    public SummarizeActivityCommandHandler(
        IRepository<Activity> activityRepository,
        ITextAiService textAiService)
    {
        _activityRepository = activityRepository;
        _textAiService = textAiService;
    }

    public async Task<ActivitySummaryDto> Handle(SummarizeActivityCommand command, CancellationToken cancellationToken)
    {
        // Business logic: Load activity
        var activity = await _activityRepository.GetByIdAsync(command.ActivityId, cancellationToken);
        if (activity == null)
            throw new KeyNotFoundException($"Activity with id {command.ActivityId} not found");

        // Use AI service (integration) to generate summary
        var summary = await _textAiService.SummarizeAsync(activity.Subject, activity.Body, cancellationToken);

        // Business logic: Update activity with AI-generated summary
        var aiMetadata = System.Text.Json.JsonSerializer.Serialize(new { Provider = "local", Model = "heuristic-1" });
        activity.UpdateSummary(summary, aiMetadata);

        await _activityRepository.UpdateAsync(activity, cancellationToken);

        // Return application DTO
        return new ActivitySummaryDto(activity.Id, summary, aiMetadata);
    }
}