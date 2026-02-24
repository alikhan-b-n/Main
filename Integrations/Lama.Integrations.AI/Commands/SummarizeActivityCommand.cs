using Lama.Integrations.AI.Interfaces;
using Lama.Integrations.AI.Queries;
using MediatR;

namespace Lama.Integrations.AI.Commands;

public record SummarizeActivityCommand(Guid ActivityId) : IRequest<ActivitySummaryDto>;

public class SummarizeActivityCommandHandler : IRequestHandler<SummarizeActivityCommand, ActivitySummaryDto>
{
    private readonly IActivityRepository _activityRepository;
    private readonly ITextAiService _textAiService;

    public SummarizeActivityCommandHandler(IActivityRepository activityRepository, ITextAiService textAiService)
    {
        _activityRepository = activityRepository;
        _textAiService = textAiService;
    }

    public async Task<ActivitySummaryDto> Handle(SummarizeActivityCommand command, CancellationToken cancellationToken)
    {
        var activity = await _activityRepository.GetByIdAsync(command.ActivityId, cancellationToken);
        if (activity == null)
            throw new KeyNotFoundException($"Activity with id {command.ActivityId} not found");

        var summary = await _textAiService.SummarizeAsync(activity.Subject, activity.Body, cancellationToken);

        // AiMetadata can include model info; for local summarizer we set a simple metadata JSON
        var aiMetadata = System.Text.Json.JsonSerializer.Serialize(new { Provider = "local", Model = "heuristic-1" });

        activity.UpdateSummary(summary, aiMetadata);

        await _activityRepository.UpdateAsync(activity, cancellationToken);

        return new ActivitySummaryDto(activity.Id, summary, aiMetadata);
    }
}