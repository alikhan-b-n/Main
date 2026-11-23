using Lama.Application.Expenses;
using Lama.Domain.Expenses.Entities;
using Lama.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Lama.Infrastructure.Repositories;

public class ExpenseRepository : IExpenseRepository
{
    private readonly ApplicationDbContext _context;

    public ExpenseRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<Expense>> ListAsync(CancellationToken cancellationToken = default) =>
        await _context.Expenses.AsNoTracking().ToListAsync(cancellationToken);

    public Task<Expense?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.Expenses.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    public void Add(Expense expense) => _context.Expenses.Add(expense);

    public void Remove(Expense expense) => _context.Expenses.Remove(expense);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);
}
