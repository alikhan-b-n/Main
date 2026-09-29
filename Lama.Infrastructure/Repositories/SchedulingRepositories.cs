using Lama.Application.LeadManagement;
using Lama.Application.Scheduling;
using Lama.Domain.Scheduling.Entities;
using Lama.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Lama.Infrastructure.Repositories;

public class AppointmentRepository : IAppointmentRepository
{
    private readonly ApplicationDbContext _context;

    public AppointmentRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<Appointment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.Appointments.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Appointment>> GetScheduledBetweenAsync(
        DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default) =>
        await _context.Appointments
            .Where(a => a.Status == AppointmentStatus.Scheduled && a.StartsAtUtc >= fromUtc && a.StartsAtUtc < toUtc)
            .OrderBy(a => a.StartsAtUtc)
            .ToListAsync(cancellationToken);

    public Task<Appointment?> FindScheduledForLeadAsync(Guid leadId, CancellationToken cancellationToken = default) =>
        _context.Appointments
            .Where(a => a.LeadId == leadId && a.Status == AppointmentStatus.Scheduled)
            .OrderBy(a => a.StartsAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<PagedResult<Appointment>> SearchAsync(
        AppointmentFilter filter, CancellationToken cancellationToken = default)
    {
        var query = _context.Appointments.AsQueryable();

        if (filter.Status.HasValue)
            query = query.Where(a => a.Status == filter.Status.Value);
        if (filter.Kind.HasValue)
            query = query.Where(a => a.Kind == filter.Kind.Value);
        if (filter.LeadId.HasValue)
            query = query.Where(a => a.LeadId == filter.LeadId.Value);
        if (filter.FromUtc.HasValue)
            query = query.Where(a => a.StartsAtUtc >= filter.FromUtc.Value);
        if (filter.ToUtc.HasValue)
            query = query.Where(a => a.StartsAtUtc < filter.ToUtc.Value);

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = $"%{filter.Search.Trim()}%";
            query = query.Where(a =>
                EF.Functions.ILike(a.FullName, search) ||
                EF.Functions.ILike(a.Email.Value, search) ||
                (a.Phone != null && EF.Functions.ILike(a.Phone, search)) ||
                (a.TelegramUsername != null && EF.Functions.ILike(a.TelegramUsername, search)));
        }

        var total = await query.CountAsync(cancellationToken);

        query = filter.UpcomingFirst
            ? query.OrderBy(a => a.StartsAtUtc)
            : query.OrderByDescending(a => a.StartsAtUtc);

        var items = await query
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Appointment>(items, filter.Page, filter.PageSize, total);
    }

    public async Task AddAsync(Appointment appointment, CancellationToken cancellationToken = default)
    {
        await _context.Appointments.AddAsync(appointment, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);
}

public class AvailabilitySettingsRepository : IAvailabilitySettingsRepository
{
    private readonly ApplicationDbContext _context;

    public AvailabilitySettingsRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>Creates the row with IconicU's default schedule the first time it is needed.</summary>
    public async Task<AvailabilitySettings> GetAsync(CancellationToken cancellationToken = default)
    {
        var settings = await _context.AvailabilitySettings
            .FirstOrDefaultAsync(s => s.Id == Domain.Scheduling.Entities.AvailabilitySettings.SingletonId,
                cancellationToken);

        if (settings != null)
            return settings;

        settings = Domain.Scheduling.Entities.AvailabilitySettings.Default();
        await _context.AvailabilitySettings.AddAsync(settings, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return settings;
    }

    public Task SaveAsync(AvailabilitySettings settings, CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);
}
