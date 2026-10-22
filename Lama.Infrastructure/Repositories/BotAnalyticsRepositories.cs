using Lama.Application.BotAnalytics;
using Lama.Domain.BotAnalytics;
using Lama.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Lama.Infrastructure.Repositories;

public class SurveyEventRepository : ISurveyEventRepository
{
    private readonly ApplicationDbContext _context;

    public SurveyEventRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<int> AddNewAsync(IReadOnlyCollection<SurveyEvent> events, CancellationToken cancellationToken = default)
    {
        if (events.Count == 0)
            return 0;

        // The bot resends a batch when it did not hear back; rows already stored are skipped
        var ids = events.Select(e => e.Id).ToList();
        var known = await _context.SurveyEvents
            .Where(e => ids.Contains(e.Id))
            .Select(e => e.Id)
            .ToListAsync(cancellationToken);

        var fresh = events
            .Where(e => !known.Contains(e.Id))
            .DistinctBy(e => e.Id)
            .ToList();
        if (fresh.Count == 0)
            return 0;

        await _context.SurveyEvents.AddRangeAsync(fresh, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return fresh.Count;
    }

    public async Task<IReadOnlyList<SurveyEvent>> GetBetweenAsync(
        DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default) =>
        await _context.SurveyEvents
            .AsNoTracking()
            .Where(e => e.OccurredAt >= fromUtc && e.OccurredAt <= toUtc)
            .ToListAsync(cancellationToken);

    public async Task<DateTime?> GetFirstEventAtAsync(CancellationToken cancellationToken = default) =>
        await _context.SurveyEvents.MinAsync(e => (DateTime?)e.OccurredAt, cancellationToken);
}

public class BotReminderSettingsRepository : IBotReminderSettingsRepository
{
    private readonly ApplicationDbContext _context;

    public BotReminderSettingsRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>Creates the row with the defaults the first time it is needed.</summary>
    public async Task<BotReminderSettings> GetAsync(CancellationToken cancellationToken = default)
    {
        var settings = await _context.BotReminderSettings
            .FirstOrDefaultAsync(s => s.Id == BotReminderSettings.SingletonId, cancellationToken);
        if (settings != null)
            return settings;

        settings = BotReminderSettings.Default();
        await _context.BotReminderSettings.AddAsync(settings, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return settings;
    }

    public Task SaveAsync(BotReminderSettings settings, CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);
}
