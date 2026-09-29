using Lama.Application.LeadManagement;
using Lama.Application.Scheduling;
using Lama.Domain.LeadManagement.Entities;
using Lama.Domain.Scheduling;
using Lama.Domain.Scheduling.Entities;

namespace Lama.Tests.Scheduling;

/// <summary>A calendar that records what it was asked to do instead of calling Google.</summary>
public sealed class FakeCalendar : ISchedulingCalendar
{
    public bool IsConfigured { get; set; } = true;
    public List<BusyInterval> Busy { get; } = new();
    public List<CalendarEventRequest> Created { get; } = new();
    public List<string> Cancelled { get; } = new();
    public List<(string EventId, DateTime StartUtc)> Moved { get; } = new();

    /// <summary>Set to simulate Google being down.</summary>
    public Exception? Failure { get; set; }

    public Task<IReadOnlyList<BusyInterval>> GetBusyAsync(
        DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default)
    {
        if (Failure != null)
            throw Failure;

        IReadOnlyList<BusyInterval> busy = Busy
            .Where(b => b.EndUtc > fromUtc && b.StartUtc < toUtc)
            .ToList();
        return Task.FromResult(busy);
    }

    public Task<CalendarEvent> CreateAsync(CalendarEventRequest request, CancellationToken cancellationToken = default)
    {
        if (Failure != null)
            throw Failure;

        Created.Add(request);
        var id = $"evt-{Created.Count}";
        Busy.Add(new BusyInterval(request.StartUtc, request.StartUtc.AddMinutes(request.DurationMinutes)));
        return Task.FromResult(new CalendarEvent(
            id, $"https://meet.google.com/{id}", $"https://calendar.google.com/{id}"));
    }

    public Task<CalendarEvent> MoveAsync(
        string eventId, DateTime startUtc, int durationMinutes, CancellationToken cancellationToken = default)
    {
        if (Failure != null)
            throw Failure;

        Moved.Add((eventId, startUtc));
        return Task.FromResult(new CalendarEvent(
            eventId, $"https://meet.google.com/{eventId}", $"https://calendar.google.com/{eventId}"));
    }

    public Task CancelAsync(string eventId, CancellationToken cancellationToken = default)
    {
        if (Failure != null)
            throw Failure;

        Cancelled.Add(eventId);
        return Task.CompletedTask;
    }
}

public sealed class InMemoryAppointmentRepository : IAppointmentRepository
{
    public List<Appointment> Appointments { get; } = new();
    public int SaveCount { get; private set; }

    public Task<Appointment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(Appointments.FirstOrDefault(a => a.Id == id));

    public Task<IReadOnlyList<Appointment>> GetScheduledBetweenAsync(
        DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Appointment> found = Appointments
            .Where(a => a.Status == AppointmentStatus.Scheduled && a.StartsAtUtc >= fromUtc && a.StartsAtUtc < toUtc)
            .OrderBy(a => a.StartsAtUtc)
            .ToList();
        return Task.FromResult(found);
    }

    public Task<Appointment?> FindScheduledForLeadAsync(Guid leadId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Appointments
            .FirstOrDefault(a => a.LeadId == leadId && a.Status == AppointmentStatus.Scheduled));

    public Task<PagedResult<Appointment>> SearchAsync(
        AppointmentFilter filter, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task AddAsync(Appointment appointment, CancellationToken cancellationToken = default)
    {
        Appointments.Add(appointment);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SaveCount++;
        return Task.CompletedTask;
    }
}

public sealed class InMemorySettingsRepository : IAvailabilitySettingsRepository
{
    public AvailabilitySettings Settings { get; set; } = AvailabilitySettings.Default();

    public Task<AvailabilitySettings> GetAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(Settings);

    public Task SaveAsync(AvailabilitySettings settings, CancellationToken cancellationToken = default)
    {
        Settings = settings;
        return Task.CompletedTask;
    }
}

public sealed class FakeLeadRepository : ILeadRepository
{
    public List<Lead> Leads { get; } = new();
    public int SaveCount { get; private set; }

    public Task<Lead?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(Leads.FirstOrDefault(l => l.Id == id));

    public Task<Lead?> FindByExternalIdAsync(string externalId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Leads.FirstOrDefault(l => l.ExternalId == externalId));

    public Task<Lead?> FindOpenDuplicateAsync(
        long? telegramId, string email, CancellationToken cancellationToken = default) =>
        Task.FromResult<Lead?>(null);

    public Task<PagedResult<Lead>> SearchAsync(LeadFilter filter, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task<IReadOnlyList<LeadStatsRow>> GetStatsRowsAsync(CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task AddAsync(Lead lead, CancellationToken cancellationToken = default)
    {
        Leads.Add(lead);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SaveCount++;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Lead lead, CancellationToken cancellationToken = default)
    {
        Leads.Remove(lead);
        return Task.CompletedTask;
    }
}
