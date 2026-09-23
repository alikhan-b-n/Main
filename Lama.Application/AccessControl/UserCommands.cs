using Lama.Application.Common;
using Lama.Domain.AccessControl.Entities;

namespace Lama.Application.AccessControl;

internal static class UserMapping
{
    public static UserDto ToDto(this CrmUser user) => new(
        user.Id,
        user.Email.Value,
        user.FullName,
        user.Role.ToString(),
        user.IsActive,
        DateTime.SpecifyKind(user.CreatedAt, DateTimeKind.Utc),
        user.LastLoginAt is { } at ? DateTime.SpecifyKind(at, DateTimeKind.Utc) : null
    );
}

/// <summary>Checks the credentials and returns the user, or null when they don't match.</summary>
public record AuthenticateUserCommand(string Email, string Password) : ICommand<UserDto?>;

public class AuthenticateUserCommandHandler : ICommandHandler<AuthenticateUserCommand, UserDto?>
{
    private readonly IUserRepository _users;
    private readonly IPasswordHasher _hasher;

    public AuthenticateUserCommandHandler(IUserRepository users, IPasswordHasher hasher)
    {
        _users = users;
        _hasher = hasher;
    }

    public async Task<UserDto?> Handle(AuthenticateUserCommand command, CancellationToken cancellationToken)
    {
        var user = await _users.FindByEmailAsync(command.Email, cancellationToken);

        // Same answer for "no such user", "wrong password" and "deactivated":
        // the sign-in form must not reveal which accounts exist.
        if (user == null || !user.IsActive || !_hasher.Verify(command.Password, user.PasswordHash))
            return null;

        user.RecordLogin();
        await _users.SaveChangesAsync(cancellationToken);
        return user.ToDto();
    }
}

public record CreateUserCommand(string Email, string FullName, string Password, UserRole Role) : ICommand<UserDto>;

public class CreateUserCommandHandler : ICommandHandler<CreateUserCommand, UserDto>
{
    private readonly IUserRepository _users;
    private readonly IPasswordHasher _hasher;

    public CreateUserCommandHandler(IUserRepository users, IPasswordHasher hasher)
    {
        _users = users;
        _hasher = hasher;
    }

    public async Task<UserDto> Handle(CreateUserCommand command, CancellationToken cancellationToken)
    {
        if (await _users.FindByEmailAsync(command.Email, cancellationToken) != null)
            throw new EmailAlreadyUsedException(command.Email);

        var user = CrmUser.Create(command.Email, command.FullName, _hasher.Hash(command.Password), command.Role);
        await _users.AddAsync(user, cancellationToken);
        return user.ToDto();
    }
}

public record UpdateUserCommand(Guid Id, string FullName, UserRole Role, bool IsActive) : ICommand;

public class UpdateUserCommandHandler : ICommandHandler<UpdateUserCommand>
{
    private readonly IUserRepository _users;

    public UpdateUserCommandHandler(IUserRepository users)
    {
        _users = users;
    }

    public async Task Handle(UpdateUserCommand command, CancellationToken cancellationToken)
    {
        var user = await _users.GetByIdAsync(command.Id, cancellationToken)
                   ?? throw new UserNotFoundException(command.Id);

        var losesAdminRights = user.IsAdmin && user.IsActive && (command.Role != UserRole.Admin || !command.IsActive);
        if (losesAdminRights && await _users.CountActiveAdminsAsync(cancellationToken) <= 1)
            throw new LastAdminException();

        user.Rename(command.FullName);
        user.ChangeRole(command.Role);
        user.SetActive(command.IsActive);
        await _users.SaveChangesAsync(cancellationToken);
    }
}

/// <summary>An administrator sets a new password for someone else.</summary>
public record ResetUserPasswordCommand(Guid Id, string NewPassword) : ICommand;

public class ResetUserPasswordCommandHandler : ICommandHandler<ResetUserPasswordCommand>
{
    private readonly IUserRepository _users;
    private readonly IPasswordHasher _hasher;

    public ResetUserPasswordCommandHandler(IUserRepository users, IPasswordHasher hasher)
    {
        _users = users;
        _hasher = hasher;
    }

    public async Task Handle(ResetUserPasswordCommand command, CancellationToken cancellationToken)
    {
        var user = await _users.GetByIdAsync(command.Id, cancellationToken)
                   ?? throw new UserNotFoundException(command.Id);

        user.ChangePassword(_hasher.Hash(command.NewPassword));
        await _users.SaveChangesAsync(cancellationToken);
    }
}

/// <summary>Signed-in user changes their own password; the current one must match.</summary>
public record ChangeOwnPasswordCommand(Guid Id, string CurrentPassword, string NewPassword) : ICommand<bool>;

public class ChangeOwnPasswordCommandHandler : ICommandHandler<ChangeOwnPasswordCommand, bool>
{
    private readonly IUserRepository _users;
    private readonly IPasswordHasher _hasher;

    public ChangeOwnPasswordCommandHandler(IUserRepository users, IPasswordHasher hasher)
    {
        _users = users;
        _hasher = hasher;
    }

    public async Task<bool> Handle(ChangeOwnPasswordCommand command, CancellationToken cancellationToken)
    {
        var user = await _users.GetByIdAsync(command.Id, cancellationToken)
                   ?? throw new UserNotFoundException(command.Id);

        if (!_hasher.Verify(command.CurrentPassword, user.PasswordHash))
            return false;

        user.ChangePassword(_hasher.Hash(command.NewPassword));
        await _users.SaveChangesAsync(cancellationToken);
        return true;
    }
}

public record DeleteUserCommand(Guid Id) : ICommand;

public class DeleteUserCommandHandler : ICommandHandler<DeleteUserCommand>
{
    private readonly IUserRepository _users;

    public DeleteUserCommandHandler(IUserRepository users)
    {
        _users = users;
    }

    public async Task Handle(DeleteUserCommand command, CancellationToken cancellationToken)
    {
        var user = await _users.GetByIdAsync(command.Id, cancellationToken)
                   ?? throw new UserNotFoundException(command.Id);

        if (user.IsAdmin && user.IsActive && await _users.CountActiveAdminsAsync(cancellationToken) <= 1)
            throw new LastAdminException();

        await _users.DeleteAsync(user, cancellationToken);
    }
}

public record GetUsersQuery : IQuery<IReadOnlyList<UserDto>>;

public class GetUsersQueryHandler : IQueryHandler<GetUsersQuery, IReadOnlyList<UserDto>>
{
    private readonly IUserRepository _users;

    public GetUsersQueryHandler(IUserRepository users)
    {
        _users = users;
    }

    public async Task<IReadOnlyList<UserDto>> Handle(GetUsersQuery query, CancellationToken cancellationToken)
    {
        var users = await _users.ListAsync(cancellationToken);
        return users.Select(u => u.ToDto()).ToList();
    }
}

public record GetUserByIdQuery(Guid Id) : IQuery<UserDto?>;

public class GetUserByIdQueryHandler : IQueryHandler<GetUserByIdQuery, UserDto?>
{
    private readonly IUserRepository _users;

    public GetUserByIdQueryHandler(IUserRepository users)
    {
        _users = users;
    }

    public async Task<UserDto?> Handle(GetUserByIdQuery query, CancellationToken cancellationToken)
    {
        var user = await _users.GetByIdAsync(query.Id, cancellationToken);
        return user?.ToDto();
    }
}
