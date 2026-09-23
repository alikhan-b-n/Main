using Lama.Application.AccessControl;
using Lama.Domain.AccessControl.Entities;
using Lama.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Lama.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly ApplicationDbContext _context;

    public UserRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<CrmUser?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.CrmUsers.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public Task<CrmUser?> FindByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalized = (email ?? string.Empty).Trim().ToLower();
        return _context.CrmUsers.FirstOrDefaultAsync(u => u.Email.Value.ToLower() == normalized, cancellationToken);
    }

    public async Task<IReadOnlyList<CrmUser>> ListAsync(CancellationToken cancellationToken = default) =>
        await _context.CrmUsers
            .AsNoTracking()
            .OrderBy(u => u.FullName)
            .ToListAsync(cancellationToken);

    public Task<int> CountActiveAdminsAsync(CancellationToken cancellationToken = default) =>
        _context.CrmUsers.CountAsync(u => u.IsActive && u.Role == UserRole.Admin, cancellationToken);

    public async Task AddAsync(CrmUser user, CancellationToken cancellationToken = default)
    {
        await _context.CrmUsers.AddAsync(user, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);

    public async Task DeleteAsync(CrmUser user, CancellationToken cancellationToken = default)
    {
        _context.CrmUsers.Remove(user);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
