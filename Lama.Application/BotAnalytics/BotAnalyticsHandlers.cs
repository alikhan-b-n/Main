using FluentValidation;
using Lama.Application.Common;
using Lama.Domain.BotAnalytics;

namespace Lama.Application.BotAnalytics;

// --- Events from the bot ----------------------------------------------------

/// <summary>One event as the bot sends it; Kind is a name such as "question_shown".</summary>
public record SurveyEventInput(
    Guid Id,
    string SessionId,
    long TelegramId,
    string Kind,
    DateTime OccurredAt,
    string? TelegramUsername = null,
    string? TelegramName = null,
    string? Source = null,
    string? QuestionId = null,
    int? Step = null,
    string? Detail = null);

public record RecordSurveyEventsCommand(IReadOnlyList<SurveyEventInput> Events) : ICommand<RecordSurveyEventsResult>;

/// <param name="Stored">New events written.</param>
/// <param name="Ignored">Unknown kinds or broken rows, dropped rather than failing the batch.</param>
public record RecordSurveyEventsResult(int Stored, int Ignored);

public class RecordSurveyEventsCommandHandler : ICommandHandler<RecordSurveyEventsCommand, RecordSurveyEventsResult>
{
    private readonly ISurveyEventRepository _events;

    public RecordSurveyEventsCommandHandler(ISurveyEventRepository events)
    {
        _events = events;
    }

    public async Task<RecordSurveyEventsResult> Handle(
        RecordSurveyEventsCommand command, CancellationToken cancellationToken)
    {
        var parsed = new List<SurveyEvent>();
        foreach (var input in command.Events)
        {
            // A newer bot may send kinds this CRM does not know yet; statistics lose
            // that row, the rest of the batch still counts
            if (!TryParseKind(input.Kind, out var kind) || input.Id == Guid.Empty
                || string.IsNullOrWhiteSpace(input.SessionId))
                continue;

            parsed.Add(SurveyEvent.Record(
                input.Id, input.SessionId, input.TelegramId, kind, input.OccurredAt,
                input.TelegramUsername, input.TelegramName, input.Source,
                input.QuestionId, input.Step, input.Detail));
        }

        var stored = await _events.AddNewAsync(parsed, cancellationToken);
        return new RecordSurveyEventsResult(stored, command.Events.Count - parsed.Count);
    }

    /// <summary>Accepts the bot's snake_case ("question_shown") as well as "QuestionShown".</summary>
    public static bool TryParseKind(string? value, out SurveyEventKind kind)
    {
        kind = default;
        return !string.IsNullOrWhiteSpace(value)
               && Enum.TryParse(value.Replace("_", string.Empty), ignoreCase: true, out kind)
               && Enum.IsDefined(kind);
    }
}

public class RecordSurveyEventsCommandValidator : AbstractValidator<RecordSurveyEventsCommand>
{
    public const int MaxBatch = 500;

    public RecordSurveyEventsCommandValidator()
    {
        RuleFor(x => x.Events).NotNull()
            .Must(e => e.Count <= MaxBatch).WithMessage($"No more than {MaxBatch} events per request");
    }
}

// --- Statistics ---------------------------------------------------------------

/// <param name="TimeZoneId">The viewer's zone, for the "when people fill it in" chart.</param>
public record GetBotStatsQuery(int Days, string? Source, string? TimeZoneId) : IQuery<BotStatsDto>;

public class GetBotStatsQueryHandler : IQueryHandler<GetBotStatsQuery, BotStatsDto>
{
    private readonly ISurveyEventRepository _events;
    private readonly TimeProvider _clock;

    public GetBotStatsQueryHandler(ISurveyEventRepository events, TimeProvider clock)
    {
        _events = events;
        _clock = clock;
    }

    public async Task<BotStatsDto> Handle(GetBotStatsQuery query, CancellationToken cancellationToken)
    {
        var now = _clock.GetUtcNow().UtcDateTime;
        // Two periods for the trend; a session that began just before keeps its later events
        var events = await _events.GetBetweenAsync(now.AddDays(-2 * query.Days), now, cancellationToken);
        var since = await _events.GetFirstEventAtAsync(cancellationToken);

        return BotStatsCalculator.Calculate(
            events, now, query.Days, query.Source, ViewerZone(query.TimeZoneId), since);
    }

    private static TimeZoneInfo ViewerZone(string? id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return TimeZoneInfo.Utc;
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(id);
        }
        catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            return TimeZoneInfo.Utc;
        }
    }
}

public class GetBotStatsQueryValidator : AbstractValidator<GetBotStatsQuery>
{
    public GetBotStatsQueryValidator()
    {
        RuleFor(x => x.Days).InclusiveBetween(1, 365);
        RuleFor(x => x.Source).MaximumLength(SurveyEvent.MaxCodeLength);
        RuleFor(x => x.TimeZoneId).MaximumLength(64);
    }
}

// --- Reminder settings -------------------------------------------------------

public record GetBotReminderSettingsQuery : IQuery<BotReminderSettingsDto>;

public class GetBotReminderSettingsQueryHandler : IQueryHandler<GetBotReminderSettingsQuery, BotReminderSettingsDto>
{
    private readonly IBotReminderSettingsRepository _settings;

    public GetBotReminderSettingsQueryHandler(IBotReminderSettingsRepository settings)
    {
        _settings = settings;
    }

    public async Task<BotReminderSettingsDto> Handle(
        GetBotReminderSettingsQuery query, CancellationToken cancellationToken) =>
        ToDto(await _settings.GetAsync(cancellationToken));

    public static BotReminderSettingsDto ToDto(BotReminderSettings s) =>
        new(s.Enabled, s.DelayHours, s.QuietFrom.ToString("HH:mm"), s.QuietTo.ToString("HH:mm"), s.TimeZoneId);
}

public record UpdateBotReminderSettingsCommand(
    bool Enabled, int DelayHours, TimeOnly QuietFrom, TimeOnly QuietTo, string TimeZoneId)
    : ICommand<BotReminderSettingsDto>;

public class UpdateBotReminderSettingsCommandHandler
    : ICommandHandler<UpdateBotReminderSettingsCommand, BotReminderSettingsDto>
{
    private readonly IBotReminderSettingsRepository _settings;

    public UpdateBotReminderSettingsCommandHandler(IBotReminderSettingsRepository settings)
    {
        _settings = settings;
    }

    public async Task<BotReminderSettingsDto> Handle(
        UpdateBotReminderSettingsCommand command, CancellationToken cancellationToken)
    {
        var settings = await _settings.GetAsync(cancellationToken);
        settings.Update(command.Enabled, command.DelayHours, command.QuietFrom, command.QuietTo, command.TimeZoneId);
        await _settings.SaveAsync(settings, cancellationToken);
        return GetBotReminderSettingsQueryHandler.ToDto(settings);
    }
}
