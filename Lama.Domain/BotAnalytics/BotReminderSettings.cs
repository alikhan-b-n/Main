using Lama.Domain.Common;

namespace Lama.Domain.BotAnalytics;

/// <summary>
/// Whether, and when, the bot nudges someone who stopped halfway through the survey.
/// One row, edited by admins in the CRM; the bot reads it and does the sending.
/// The bot writes once per survey, so there is no count to configure.
/// </summary>
public class BotReminderSettings : AggregateRoot
{
    public static readonly Guid SingletonId = new("9f1d2c4e-0000-4000-8000-000000000002");

    public const int MinDelayHours = 1;
    public const int MaxDelayHours = 72;

    public bool Enabled { get; private set; }

    /// <summary>How long after the last answer the reminder goes out.</summary>
    public int DelayHours { get; private set; }

    /// <summary>Nothing is sent between these two times; a reminder due then waits for the morning.</summary>
    public TimeOnly QuietFrom { get; private set; }
    public TimeOnly QuietTo { get; private set; }

    /// <summary>IANA zone the quiet hours are in. Applicants are mostly in Kazakhstan.</summary>
    public string TimeZoneId { get; private set; }

    private BotReminderSettings()
    {
        TimeZoneId = null!;
    }

    public static BotReminderSettings Default() =>
        Create(enabled: true, delayHours: 3, quietFrom: new TimeOnly(22, 0), quietTo: new TimeOnly(9, 0),
            timeZoneId: "Asia/Almaty");

    public static BotReminderSettings Create(
        bool enabled, int delayHours, TimeOnly quietFrom, TimeOnly quietTo, string timeZoneId)
    {
        var settings = new BotReminderSettings { Id = SingletonId };
        settings.Apply(enabled, delayHours, quietFrom, quietTo, timeZoneId);
        return settings;
    }

    public void Update(bool enabled, int delayHours, TimeOnly quietFrom, TimeOnly quietTo, string timeZoneId)
    {
        Apply(enabled, delayHours, quietFrom, quietTo, timeZoneId);
        UpdatedAt = DateTime.UtcNow;
    }

    private void Apply(bool enabled, int delayHours, TimeOnly quietFrom, TimeOnly quietTo, string timeZoneId)
    {
        if (delayHours is < MinDelayHours or > MaxDelayHours)
            throw new ArgumentException(
                $"The delay must be between {MinDelayHours} and {MaxDelayHours} hours", nameof(delayHours));
        if (string.IsNullOrWhiteSpace(timeZoneId))
            throw new ArgumentException("Time zone is required", nameof(timeZoneId));

        try
        {
            TimeZoneInfo.FindSystemTimeZoneById(timeZoneId.Trim());
        }
        catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            throw new ArgumentException($"Unknown time zone: {timeZoneId}", nameof(timeZoneId));
        }

        Enabled = enabled;
        DelayHours = delayHours;
        QuietFrom = quietFrom;
        QuietTo = quietTo;
        TimeZoneId = timeZoneId.Trim();
    }
}
