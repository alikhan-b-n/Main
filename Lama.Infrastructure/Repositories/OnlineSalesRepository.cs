using Lama.Application.OnlineSales;
using Lama.Domain.OnlineSales.Entities;
using Lama.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Lama.Infrastructure.Repositories;

public class OnlineSalesRepository : IOnlineSalesRepository
{
    private readonly ApplicationDbContext _context;

    public OnlineSalesRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<ShopifyConnection?> GetConnectionAsync(CancellationToken cancellationToken = default) =>
        _context.ShopifyConnections.OrderBy(c => c.CreatedAt).FirstOrDefaultAsync(cancellationToken);

    public async Task AddConnectionAsync(ShopifyConnection connection, CancellationToken cancellationToken = default)
    {
        await _context.ShopifyConnections.AddAsync(connection, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyDictionary<string, OnlineOrder>> GetOrdersAsync(
        IEnumerable<string> shopifyIds, CancellationToken cancellationToken = default)
    {
        var ids = shopifyIds.Distinct().ToList();
        if (ids.Count == 0)
            return new Dictionary<string, OnlineOrder>();

        return await _context.OnlineOrders
            .Where(o => ids.Contains(o.ShopifyId))
            .ToDictionaryAsync(o => o.ShopifyId, cancellationToken);
    }

    public async Task<IReadOnlyList<OnlineOrder>> ListOrdersAsync(CancellationToken cancellationToken = default) =>
        await _context.OnlineOrders.AsNoTracking().ToListAsync(cancellationToken);

    public Task<int> CountOrdersAsync(CancellationToken cancellationToken = default) =>
        _context.OnlineOrders.CountAsync(o => !o.Cancelled, cancellationToken);

    public void AddOrder(OnlineOrder order) => _context.OnlineOrders.Add(order);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);
}
