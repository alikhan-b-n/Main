using Lama.Application.AccessControl;
using Lama.Domain.AccessControl.Entities;
using Lama.Infrastructure.Security;

namespace Lama.Tests.AccessControl;

public class UserCommandTests
{
    private readonly InMemoryUserRepository _users = new();
    private readonly Pbkdf2PasswordHasher _hasher = new();

    private Task<UserDto> CreateUser(string email, string name, string password, UserRole role) =>
        new CreateUserCommandHandler(_users, _hasher)
            .Handle(new CreateUserCommand(email, name, password, role), CancellationToken.None);

    private Task<UserDto?> Login(string email, string password) =>
        new AuthenticateUserCommandHandler(_users, _hasher)
            .Handle(new AuthenticateUserCommand(email, password), CancellationToken.None);

    private Task Update(Guid id, string name, UserRole role, bool isActive) =>
        new UpdateUserCommandHandler(_users)
            .Handle(new UpdateUserCommand(id, name, role, isActive), CancellationToken.None);

    private Task Delete(Guid id) =>
        new DeleteUserCommandHandler(_users).Handle(new DeleteUserCommand(id), CancellationToken.None);

    [Fact]
    public async Task CreateUser_StoresAHashAndLetsThePersonSignIn()
    {
        var created = await CreateUser("Manager@IconicU.kz", "  Айгерим Сапарова ", "manager-password", UserRole.Manager);

        Assert.Equal("Manager@IconicU.kz", created.Email);
        Assert.Equal("Айгерим Сапарова", created.FullName);
        Assert.Equal("Manager", created.Role);
        Assert.True(created.IsActive);
        Assert.NotEqual("manager-password", _users.Stored.Single().PasswordHash);

        var signedIn = await Login("manager@iconicu.kz", "manager-password"); // email is case-insensitive
        Assert.NotNull(signedIn);
        Assert.Equal(created.Id, signedIn!.Id);
        Assert.NotNull(_users.Stored.Single().LastLoginAt);
    }

    [Fact]
    public async Task CreateUser_RejectsADuplicateEmail()
    {
        await CreateUser("dup@iconicu.kz", "First Person", "password-one", UserRole.Manager);

        await Assert.ThrowsAsync<EmailAlreadyUsedException>(
            () => CreateUser("DUP@iconicu.kz", "Second Person", "password-two", UserRole.Admin));
    }

    [Fact]
    public async Task Login_FailsOnWrongPasswordOrDeactivatedAccount()
    {
        var user = await CreateUser("manager@iconicu.kz", "Manager Person", "manager-password", UserRole.Manager);

        Assert.Null(await Login("manager@iconicu.kz", "wrong-password"));
        Assert.Null(await Login("nobody@iconicu.kz", "manager-password"));

        await Update(user.Id, "Manager Person", UserRole.Manager, isActive: false);
        Assert.Null(await Login("manager@iconicu.kz", "manager-password"));
    }

    [Fact]
    public async Task ChangeOwnPassword_NeedsTheCurrentOne()
    {
        var user = await CreateUser("manager@iconicu.kz", "Manager Person", "old-password", UserRole.Manager);
        var handler = new ChangeOwnPasswordCommandHandler(_users, _hasher);

        var wrong = await handler.Handle(
            new ChangeOwnPasswordCommand(user.Id, "not-the-password", "new-password"), CancellationToken.None);
        Assert.False(wrong);
        Assert.NotNull(await Login("manager@iconicu.kz", "old-password"));

        var ok = await handler.Handle(
            new ChangeOwnPasswordCommand(user.Id, "old-password", "new-password"), CancellationToken.None);
        Assert.True(ok);
        Assert.Null(await Login("manager@iconicu.kz", "old-password"));
        Assert.NotNull(await Login("manager@iconicu.kz", "new-password"));
    }

    [Fact]
    public async Task ResetPassword_LetsAnAdminSetANewOne()
    {
        var user = await CreateUser("manager@iconicu.kz", "Manager Person", "forgotten-password", UserRole.Manager);

        await new ResetUserPasswordCommandHandler(_users, _hasher)
            .Handle(new ResetUserPasswordCommand(user.Id, "issued-by-admin"), CancellationToken.None);

        Assert.Null(await Login("manager@iconicu.kz", "forgotten-password"));
        Assert.NotNull(await Login("manager@iconicu.kz", "issued-by-admin"));
    }

    [Fact]
    public async Task TheLastActiveAdminCannotBeDemotedDeactivatedOrDeleted()
    {
        var admin = await CreateUser("admin@iconicu.kz", "The Admin", "admin-password", UserRole.Admin);
        await CreateUser("manager@iconicu.kz", "Manager Person", "manager-password", UserRole.Manager);

        await Assert.ThrowsAsync<LastAdminException>(() => Update(admin.Id, "The Admin", UserRole.Manager, true));
        await Assert.ThrowsAsync<LastAdminException>(() => Update(admin.Id, "The Admin", UserRole.Admin, false));
        await Assert.ThrowsAsync<LastAdminException>(() => Delete(admin.Id));

        // With a second administrator the first one can step down
        var second = await CreateUser("admin2@iconicu.kz", "Second Admin", "admin-password-2", UserRole.Admin);
        await Update(admin.Id, "The Admin", UserRole.Manager, true);
        Assert.Equal("Manager", (await new GetUserByIdQueryHandler(_users)
            .Handle(new GetUserByIdQuery(admin.Id), CancellationToken.None))!.Role);
        Assert.True(_users.Stored.Single(u => u.Id == second.Id).IsAdmin);
    }

    [Fact]
    public async Task UnknownUserIsReportedAsNotFound()
    {
        await Assert.ThrowsAsync<UserNotFoundException>(() => Update(Guid.NewGuid(), "Nobody", UserRole.Manager, true));
        await Assert.ThrowsAsync<UserNotFoundException>(() => Delete(Guid.NewGuid()));
    }

    private sealed class InMemoryUserRepository : IUserRepository
    {
        public List<CrmUser> Stored { get; } = new();

        public Task<CrmUser?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Stored.FirstOrDefault(u => u.Id == id));

        public Task<CrmUser?> FindByEmailAsync(string email, CancellationToken cancellationToken = default) =>
            Task.FromResult(Stored.FirstOrDefault(u =>
                string.Equals(u.Email.Value, email.Trim(), StringComparison.OrdinalIgnoreCase)));

        public Task<IReadOnlyList<CrmUser>> ListAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<CrmUser>>(Stored.OrderBy(u => u.FullName).ToList());

        public Task<int> CountActiveAdminsAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(Stored.Count(u => u.IsActive && u.IsAdmin));

        public Task AddAsync(CrmUser user, CancellationToken cancellationToken = default)
        {
            Stored.Add(user);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task DeleteAsync(CrmUser user, CancellationToken cancellationToken = default)
        {
            Stored.Remove(user);
            return Task.CompletedTask;
        }
    }
}
