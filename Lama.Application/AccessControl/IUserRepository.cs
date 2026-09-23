using Lama.Domain.AccessControl.Entities;

namespace Lama.Application.AccessControl;

public interface IUserRepository
{
    Task<CrmUser?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Case-insensitive lookup — people type their email in any case.</summary>
    Task<CrmUser?> FindByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CrmUser>> ListAsync(CancellationToken cancellationToken = default);

    Task<int> CountActiveAdminsAsync(CancellationToken cancellationToken = default);

    Task AddAsync(CrmUser user, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);

    Task DeleteAsync(CrmUser user, CancellationToken cancellationToken = default);
}

/// <summary>Password hashing lives in Infrastructure; the domain only stores the hash.</summary>
public interface IPasswordHasher
{
    string Hash(string password);

    bool Verify(string password, string hash);
}

public class UserNotFoundException : Exception
{
    public UserNotFoundException(Guid id) : base($"User with ID {id} not found")
    {
    }
}

public class EmailAlreadyUsedException : Exception
{
    public EmailAlreadyUsedException(string email) : base($"User with email {email} already exists")
    {
    }
}

/// <summary>Guards against locking everyone out of user management.</summary>
public class LastAdminException : Exception
{
    public LastAdminException() : base("The last active administrator cannot be removed, demoted or deactivated")
    {
    }
}

public record UserDto(
    Guid Id,
    string Email,
    string FullName,
    string Role,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? LastLoginAt
);
