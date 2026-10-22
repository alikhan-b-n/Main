using Lama.Domain.Common;

namespace Lama.Domain.BotAnalytics;

/// <summary>
/// One step of someone's way through the Telegram bot: pressed /start, saw a question,
/// answered it, gave up. Answers themselves are not stored here — they reach the CRM
/// only with a finished survey, as a lead. These rows say how people got there.
/// </summary>
public class SurveyEvent : Entity
{
    public const int MaxCodeLength = 64;
    public const int MaxNameLength = 100;

    /// <summary>One run of the bot from /start; a second /start begins a new session.</summary>
    public string SessionId { get; private set; }

    public long TelegramId { get; private set; }
    public string? TelegramUsername { get; private set; }
    /// <summary>The name shown in Telegram — the survey asks for the real one only near the end.</summary>
    public string? TelegramName { get; private set; }

    /// <summary>Where the person came from, once known: deep link at once, or the last question.</summary>
    public string? Source { get; private set; }

    public SurveyEventKind Kind { get; private set; }

    /// <summary>Survey question code (app/survey.py), for events about a question.</summary>
    public string? QuestionId { get; private set; }
    /// <summary>Position of that question in the bot's list, so questions sort as the bot asks them.</summary>
    public int? Step { get; private set; }

    /// <summary>Short extra: the branch for a finished survey, the reason for a bad answer.</summary>
    public string? Detail { get; private set; }

    public DateTime OccurredAt { get; private set; }

    private SurveyEvent()
    {
        SessionId = null!;
    }

    /// <param name="id">Chosen by the bot, so a batch sent twice is stored once.</param>
    public static SurveyEvent Record(
        Guid id,
        string sessionId,
        long telegramId,
        SurveyEventKind kind,
        DateTime occurredAt,
        string? telegramUsername = null,
        string? telegramName = null,
        string? source = null,
        string? questionId = null,
        int? step = null,
        string? detail = null)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Event id is required", nameof(id));
        if (string.IsNullOrWhiteSpace(sessionId))
            throw new ArgumentException("Session id is required", nameof(sessionId));

        return new SurveyEvent
        {
            Id = id,
            SessionId = Clip(sessionId, MaxCodeLength)!,
            TelegramId = telegramId,
            TelegramUsername = Clip(telegramUsername, MaxCodeLength),
            TelegramName = Clip(telegramName, MaxNameLength),
            Source = Clip(source, MaxCodeLength),
            Kind = kind,
            QuestionId = Clip(questionId, MaxCodeLength),
            Step = step,
            Detail = Clip(detail, MaxCodeLength),
            OccurredAt = DateTime.SpecifyKind(occurredAt.ToUniversalTime(), DateTimeKind.Utc)
        };
    }

    // The bot is trusted but not exact; an overlong code must not lose the whole batch
    private static string? Clip(string? value, int length)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        var trimmed = value.Trim();
        return trimmed.Length <= length ? trimmed : trimmed[..length];
    }
}

public enum SurveyEventKind
{
    /// <summary>Pressed /start and saw the welcome.</summary>
    BotStarted,
    /// <summary>Pressed "start the survey".</summary>
    SurveyStarted,
    QuestionShown,
    Answered,
    Skipped,
    /// <summary>Went back from this question to the previous one.</summary>
    Back,
    /// <summary>An answer the bot refused: not an email, text instead of a button.</summary>
    InvalidInput,
    /// <summary>Reached the end; Detail holds the branch (call, consultation, potential, nurture).</summary>
    Completed,
    /// <summary>Stopped on purpose with /cancel.</summary>
    Cancelled,
    ReminderSent,
    /// <summary>Pressed "continue" under a reminder.</summary>
    Resumed,
    BookingOffered,
    BookingDayPicked,
    BookingBooked,
    BookingCancelled
}
