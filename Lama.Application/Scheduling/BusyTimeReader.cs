using Lama.Domain.Scheduling;

namespace Lama.Application.Scheduling;

/// <summary>
/// Everything that blocks a slot: the consultant's own calendar plus the calls we booked.
/// Both are needed — an event we failed to write to Google still occupies our grid, and a
/// personal event in Google is invisible to us.
/// </summary>
public class BusyTimeReader
{
    private readonly ISchedulingCalendar _calendar;
    private readonly IAppointmentRepository _appointments;

    public BusyTimeReader(ISchedulingCalendar calendar, IAppointmentRepository appointments)
    {
        _calendar = calendar;
        _appointments = appointments;
    }

    /// <summary>
    /// Throws <see cref="CalendarUnavailableException"/> when Google cannot be reached:
    /// offering slots from our own records alone would book over the consultant's day.
    /// </summary>
    /// <param name="ignoreAppointmentId">
    /// The call being rescheduled: its own slot must not count as taken.
    /// </param>
    public async Task<IReadOnlyList<BusyInterval>> ReadAsync(
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken = default,
        Guid? ignoreAppointmentId = null)
    {
        var busy = new List<BusyInterval>();

        if (_calendar.IsConfigured)
            busy.AddRange(await _calendar.GetBusyAsync(fromUtc, toUtc, cancellationToken));

        var booked = await _appointments.GetScheduledBetweenAsync(fromUtc, toUtc, cancellationToken);
        busy.AddRange(booked
            .Where(a => a.Id != ignoreAppointmentId)
            .Select(a => new BusyInterval(a.StartsAtUtc, a.EndsAtUtc)));

        return busy;
    }
}
