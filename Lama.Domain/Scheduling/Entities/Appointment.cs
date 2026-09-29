using Lama.Domain.Common;
using Lama.Domain.CustomerManagement.ValueObjects;

namespace Lama.Domain.Scheduling.Entities;

/// <summary>
/// A booked call. Mirrors an event in the consultant's Google Calendar: the event carries
/// the Meet link, this row carries who booked it, which lead it belongs to and its state,
/// so the CRM stays usable even when Google is unreachable.
/// </summary>
public class Appointment : AggregateRoot
{
    public const int MaxCommentLength = 500;

    public Guid? LeadId { get; private set; }

    // Copied from the lead: the CRM shows a booking even if the lead is deleted later
    public string FullName { get; private set; }
    public Email Email { get; private set; }
    public string? Phone { get; private set; }
    public string? TelegramUsername { get; private set; }
    public long? TelegramId { get; private set; }

    public DateTime StartsAtUtc { get; private set; }
    public int DurationMinutes { get; private set; }
    public DateTime EndsAtUtc => StartsAtUtc.AddMinutes(DurationMinutes);

    /// <summary>The zone the person saw the time in, kept for wording emails and reminders.</summary>
    public string TimeZoneId { get; private set; }

    public AppointmentKind Kind { get; private set; }
    public AppointmentStatus Status { get; private set; }

    // Google side; null while the event has not been created yet
    public string? GoogleEventId { get; private set; }
    public string? MeetUrl { get; private set; }
    public string? CalendarLink { get; private set; }

    public string? Comment { get; private set; }
    public string? CancelReason { get; private set; }
    public DateTime? CancelledAt { get; private set; }

    public bool IsActive => Status == AppointmentStatus.Scheduled;

    private Appointment()
    {
        FullName = null!;
        Email = null!;
        TimeZoneId = null!;
    }

    public static Appointment Book(
        Guid? leadId,
        string fullName,
        string email,
        DateTime startsAtUtc,
        int durationMinutes,
        string timeZoneId,
        AppointmentKind kind = AppointmentKind.IntroCall,
        string? phone = null,
        string? telegramUsername = null,
        long? telegramId = null,
        string? comment = null)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            throw new ArgumentException("Full name cannot be empty", nameof(fullName));
        if (startsAtUtc.Kind != DateTimeKind.Utc)
            throw new ArgumentException("Start must be in UTC", nameof(startsAtUtc));
        if (durationMinutes is < AvailabilitySettings.MinSlotMinutes or > AvailabilitySettings.MaxSlotMinutes)
            throw new ArgumentOutOfRangeException(nameof(durationMinutes));
        if (string.IsNullOrWhiteSpace(timeZoneId))
            throw new ArgumentException("Time zone is required", nameof(timeZoneId));

        return new Appointment
        {
            LeadId = leadId,
            FullName = fullName.Trim(),
            Email = Email.Create(email.Trim()),
            Phone = NullIfBlank(phone),
            TelegramUsername = NullIfBlank(telegramUsername),
            TelegramId = telegramId,
            StartsAtUtc = startsAtUtc,
            DurationMinutes = durationMinutes,
            TimeZoneId = timeZoneId.Trim(),
            Kind = kind,
            Status = AppointmentStatus.Scheduled,
            Comment = Trimmed(comment, MaxCommentLength, nameof(comment))
        };
    }

    /// <summary>Stores what Google gave back once the event exists.</summary>
    public void AttachCalendarEvent(string eventId, string? meetUrl, string? calendarLink)
    {
        if (string.IsNullOrWhiteSpace(eventId))
            throw new ArgumentException("Calendar event id cannot be empty", nameof(eventId));

        GoogleEventId = eventId.Trim();
        MeetUrl = NullIfBlank(meetUrl);
        CalendarLink = NullIfBlank(calendarLink);
        Touch();
    }

    public void MoveTo(DateTime startsAtUtc, int durationMinutes)
    {
        EnsureActive("rescheduled");
        if (startsAtUtc.Kind != DateTimeKind.Utc)
            throw new ArgumentException("Start must be in UTC", nameof(startsAtUtc));
        if (durationMinutes is < AvailabilitySettings.MinSlotMinutes or > AvailabilitySettings.MaxSlotMinutes)
            throw new ArgumentOutOfRangeException(nameof(durationMinutes));

        StartsAtUtc = startsAtUtc;
        DurationMinutes = durationMinutes;
        Touch();
    }

    public void Cancel(string? reason = null)
    {
        EnsureActive("cancelled");
        Status = AppointmentStatus.Cancelled;
        CancelReason = Trimmed(reason, MaxCommentLength, nameof(reason));
        CancelledAt = DateTime.UtcNow;
        Touch();
    }

    public void MarkCompleted()
    {
        EnsureActive("completed");
        Status = AppointmentStatus.Completed;
        Touch();
    }

    public void MarkNoShow()
    {
        EnsureActive("marked as a no-show");
        Status = AppointmentStatus.NoShow;
        Touch();
    }

    private void EnsureActive(string action)
    {
        if (!IsActive)
            throw new InvalidOperationException($"A {Status.ToString().ToLowerInvariant()} appointment cannot be {action}");
    }

    private void Touch() => UpdatedAt = DateTime.UtcNow;

    private static string? NullIfBlank(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? Trimmed(string? value, int maxLength, string parameterName)
    {
        var cleaned = NullIfBlank(value);
        if (cleaned != null && cleaned.Length > maxLength)
            throw new ArgumentException($"Text must not exceed {maxLength} characters", parameterName);
        return cleaned;
    }
}

public enum AppointmentKind
{
    /// <summary>Free 30-minute intro call, booked from the bot.</summary>
    IntroCall,
    /// <summary>Paid strategy consultation, booked by a manager for now.</summary>
    Consultation
}

public enum AppointmentStatus
{
    Scheduled,
    Cancelled,
    Completed,
    NoShow
}
