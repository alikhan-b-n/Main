using Lama.Application.LeadManagement;
using Lama.Domain.LeadManagement.Entities;
using Lama.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Lama.Infrastructure.Repositories;

public class LeadRepository : ILeadRepository
{
    private readonly ApplicationDbContext _context;

    public LeadRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<Lead?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.Leads
            .Include(l => l.Events)
            .FirstOrDefaultAsync(l => l.Id == id, cancellationToken);

    public Task<Lead?> FindByExternalIdAsync(string externalId, CancellationToken cancellationToken = default) =>
        _context.Leads.FirstOrDefaultAsync(l => l.ExternalId == externalId, cancellationToken);

    public Task<Lead?> FindOpenDuplicateAsync(long? telegramId, string email, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim().ToLower();

        return _context.Leads
            .Include(l => l.Events)
            .Where(l => l.Status != LeadStatus.ContractSigned && l.Status != LeadStatus.Lost)
            .Where(l => (telegramId != null && l.TelegramId == telegramId)
                        || l.Email.Value.ToLower() == normalizedEmail)
            .OrderByDescending(l => l.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<PagedResult<Lead>> SearchAsync(LeadFilter filter, CancellationToken cancellationToken = default)
    {
        var query = _context.Leads.AsNoTracking();

        if (filter.Status.HasValue)
            query = query.Where(l => l.Status == filter.Status.Value);
        if (filter.Temperature.HasValue)
            query = query.Where(l => l.Temperature == filter.Temperature.Value);
        if (!string.IsNullOrWhiteSpace(filter.Source))
            query = query.Where(l => l.Source == filter.Source);
        if (!string.IsNullOrWhiteSpace(filter.TargetDegree))
            query = query.Where(l => l.TargetDegree == filter.TargetDegree);
        if (!string.IsNullOrWhiteSpace(filter.Country))
            query = query.Where(l => l.TargetCountries.Contains(filter.Country));
        if (filter.CreatedFrom.HasValue)
            query = query.Where(l => l.CreatedAt >= filter.CreatedFrom.Value);
        if (filter.CreatedTo.HasValue)
            query = query.Where(l => l.CreatedAt < filter.CreatedTo.Value);

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var pattern = $"%{EscapeLike(filter.Search.Trim())}%";
            query = query.Where(l =>
                EF.Functions.ILike(l.FullName, pattern) ||
                EF.Functions.ILike(l.Email.Value, pattern) ||
                (l.Phone != null && EF.Functions.ILike(l.Phone, pattern)) ||
                (l.TelegramUsername != null && EF.Functions.ILike(l.TelegramUsername, pattern)));
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(l => l.CreatedAt)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Lead>(items, filter.Page, filter.PageSize, total);
    }

    public async Task<IReadOnlyList<LeadStatsRow>> GetStatsRowsAsync(CancellationToken cancellationToken = default) =>
        await _context.Leads
            .AsNoTracking()
            .Select(l => new LeadStatsRow(
                l.Status,
                l.Temperature,
                l.Source,
                l.AnnualBudget,
                l.TargetDegree,
                l.TargetCountries,
                l.CreatedAt))
            .ToListAsync(cancellationToken);

    public async Task AddAsync(Lead lead, CancellationToken cancellationToken = default)
    {
        await _context.Leads.AddAsync(lead, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);

    public async Task DeleteAsync(Lead lead, CancellationToken cancellationToken = default)
    {
        _context.Leads.Remove(lead);
        await _context.SaveChangesAsync(cancellationToken);
    }

    // ILIKE treats % and _ as wildcards; a search for "50%" should match literally.
    private static string EscapeLike(string value) =>
        value.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
}
