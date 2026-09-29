using Lama.Application.LeadManagement;
using Lama.Domain.Scheduling;
using Lama.Domain.Scheduling.Entities;

namespace Lama.Application.Scheduling;

/// <summary>
/// The consultant's calendar. Implemented over Google Calendar; kept as a port so the
/// booking rules can be tested without a network and so a different calendar could be
/// plugged in later.
/// </summary>
public interface ISchedulingCalendar
{
    /// <summary>False when the integration has no credentials yet.</summary>
    bool IsConfigured { get; }

    /// <summary>Everything already in the calendar between the two instants.</summary>
    Task<IReadOnlyList<BusyInterval>> GetBusyAsync(
        DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default);

    /// <summary>Creates the event together with its Meet link.</summary>
    Task<CalendarEvent> CreateAsync(CalendarEventRequest request, CancellationToken cancellationToken = default);

    Task<CalendarEvent> MoveAsync(
        string eventId, DateTime startUtc, int durationMinutes, CancellationToken cancellationToken = default);

    /// <summary>Removes the event. An event that is already gone is not an error.</summary>
    Task CancelAsync(string eventId, CancellationToken cancellationToken = default);
}

public record CalendarEventRequest(
    string Summary,
    string? Description,
    DateTime StartUtc,
    int DurationMinutes,
    string TimeZoneId,
    string? AttendeeEmail,
    string? AttendeeName);

public record CalendarEvent(string EventId, string? MeetUrl, string? HtmlLink);

/// <summary>Google is unreachable or rejected the request; the caller decides what to tell the person.</summary>
public class CalendarUnavailableException : Exception
{
    public CalendarUnavailableException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}

public interface IAvailabilitySettingsRepository
{
    /// <summary>The single settings row, created with the defaults the first time it is asked for.</summary>
    Task<AvailabilitySettings> GetAsync(CancellationToken cancellationToken = default);

    Task SaveAsync(AvailabilitySettings settings, CancellationToken cancellationToken = default);
}

public interface IAppointmentRepository
{
    Task<Appointment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Scheduled calls in the period — the slots our own bookings occupy.</summary>
    Task<IReadOnlyList<Appointment>> GetScheduledBetweenAsync(
        DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default);

    /// <summary>The scheduled call a lead already has, if any.</summary>
    Task<Appointment?> FindScheduledForLeadAsync(Guid leadId, CancellationToken cancellationToken = default);

    Task<PagedResult<Appointment>> SearchAsync(
        AppointmentFilter filter, CancellationToken cancellationToken = default);

    Task AddAsync(Appointment appointment, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

public record AppointmentFilter(
    AppointmentStatus? Status = null,
    AppointmentKind? Kind = null,
    Guid? LeadId = null,
    DateTime? FromUtc = null,
    DateTime? ToUtc = null,
    string? Search = null,
    bool UpcomingFirst = true,
    int Page = 1,
    int PageSize = 25
);

public class AppointmentNotFoundException : Exception
{
    public AppointmentNotFoundException(Guid id) : base($"Appointment with ID {id} not found")
    {
    }
}

/// <summary>The chosen time is not on the grid any more: taken, too soon, or outside working hours.</summary>
public class SlotUnavailableException : Exception
{
    public SlotUnavailableException(DateTime startUtc)
        : base($"The slot at {startUtc:yyyy-MM-dd HH:mm} UTC is no longer available")
    {
    }
}

/// <summary>Booking is switched off in the CRM.</summary>
public class BookingDisabledException : Exception
{
    public BookingDisabledException() : base("Online booking is currently switched off")
    {
    }
}
