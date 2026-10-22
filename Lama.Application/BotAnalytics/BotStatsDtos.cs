namespace Lama.Application.BotAnalytics;

/// <summary>Everything the "bot statistics" page shows for one period.</summary>
public record BotStatsDto(
    int Days,
    DateTime FromUtc,
    DateTime ToUtc,
    /// <summary>First event ever recorded; null while the bot has sent nothing yet.</summary>
    DateTime? CollectingSinceUtc,
    /// <summary>Whole days of data behind the numbers, never more than <see cref="Days"/>.</summary>
    int CollectedDays,
    BotStatsTotals Totals,
    /// <summary>The same numbers for the period just before, for the trend.</summary>
    BotStatsTotals PreviousTotals,
    IReadOnlyList<QuestionStatDto> Questions,
    IReadOnlyList<CountDto> Outcomes,
    IReadOnlyList<SourceStatDto> Sources,
    BookingFunnelDto Booking,
    ReminderStatDto Reminders,
    /// <summary>Surveys started in each hour of the day, 0–23, in the viewer's time zone.</summary>
    IReadOnlyList<int> StartsByHour,
    IReadOnlyList<AbandonedSessionDto> Abandoned,
    int AbandonedTotal);

public record BotStatsTotals(
    int BotStarts,
    int SurveysStarted,
    int Completed,
    int Abandoned,
    int InProgress,
    double? MedianSurveySeconds);

public record QuestionStatDto(
    string QuestionId,
    int Step,
    /// <summary>Sessions that saw the question at least once.</summary>
    int Reached,
    int Answered,
    int Skipped,
    /// <summary>Abandoned sessions whose last question was this one.</summary>
    int QuitHere,
    double? MedianSeconds,
    int InvalidInputs,
    int Backs);

public record CountDto(string Key, int Count);

public record SourceStatDto(string Source, int Started, int Completed);

public record BookingFunnelDto(int Offered, int PickedDay, int Booked, int Cancelled);

public record ReminderStatDto(int Sent, int Resumed, int CompletedAfter);

public record AbandonedSessionDto(
    string SessionId,
    long TelegramId,
    string? TelegramUsername,
    string? TelegramName,
    string? Source,
    string? LastQuestionId,
    int? LastStep,
    DateTime LastActivityAt,
    bool Cancelled,
    bool Reminded);

public record BotReminderSettingsDto(
    bool Enabled,
    int DelayHours,
    string QuietFrom,
    string QuietTo,
    string TimeZoneId);
