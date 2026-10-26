using Lama.Application.Stockists;
using Lama.Domain.Stockists.Entities;
using Lama.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Lama.Infrastructure.Repositories;

public class StockistRepository : IStockistRepository
{
    private readonly ApplicationDbContext _context;

    public StockistRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<Stockist>> ListAsync(CancellationToken cancellationToken = default) =>
        await _context.Stockists
            .AsNoTracking()
            .Include(s => s.Invoices)
            .AsSplitQuery()
            .ToListAsync(cancellationToken);

    public Task<Stockist?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.Stockists
            .Include(s => s.Invoices)
            .Include(s => s.Events)
            .AsSplitQuery()
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public async Task<IReadOnlySet<string>> GetNamesAsync(CancellationToken cancellationToken = default)
    {
        var names = await _context.Stockists.Select(s => s.Name.ToLower()).ToListAsync(cancellationToken);
        return names.ToHashSet();
    }

    public async Task AddAsync(Stockist stockist, CancellationToken cancellationToken = default)
    {
        await _context.Stockists.AddAsync(stockist, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task AddRangeAsync(IEnumerable<Stockist> stockists, CancellationToken cancellationToken = default)
    {
        await _context.Stockists.AddRangeAsync(stockists, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);

    public async Task DeleteAsync(Stockist stockist, CancellationToken cancellationToken = default)
    {
        _context.Stockists.Remove(stockist);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
