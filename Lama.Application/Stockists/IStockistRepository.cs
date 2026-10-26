using Lama.Domain.Stockists.Entities;

namespace Lama.Application.Stockists;

/// <summary>
/// A bookseller has dozens of stores, not thousands, so lists are loaded whole and
/// filtered in memory: the attention rules then live in one place (the domain).
/// </summary>
public interface IStockistRepository
{
    /// <summary>Every store and lead with its stock and invoices, read-only. No history.</summary>
    Task<IReadOnlyList<Stockist>> ListAsync(CancellationToken cancellationToken = default);

    /// <summary>One store with stock, invoices and history, tracked for updates.</summary>
    Task<Stockist?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Names already taken, lower-cased — the import skips those.</summary>
    Task<IReadOnlySet<string>> GetNamesAsync(CancellationToken cancellationToken = default);

    Task AddAsync(Stockist stockist, CancellationToken cancellationToken = default);

    Task AddRangeAsync(IEnumerable<Stockist> stockists, CancellationToken cancellationToken = default);

    /// <summary>Persists changes made to stores loaded through this repository.</summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);

    Task DeleteAsync(Stockist stockist, CancellationToken cancellationToken = default);
}

public class StockistNotFoundException : Exception
{
    public StockistNotFoundException(Guid id) : base($"Store with ID {id} not found")
    {
    }
}
