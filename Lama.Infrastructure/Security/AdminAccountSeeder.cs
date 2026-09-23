using Lama.Application.AccessControl;
using Lama.Domain.AccessControl.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lama.Infrastructure.Security;

/// <summary>
/// The administrator account comes from configuration, not from a sign-up form:
///
///   dotnet user-secrets set "Auth:Admin:Email" "you@iconicu.kz" --project Lama.Api
///   dotnet user-secrets set "Auth:Admin:Password" "&lt;long password&gt;" --project Lama.Api
///
/// Everyone else is created by that administrator inside the CRM.
/// </summary>
public class AdminAccountOptions
{
    public const string SectionName = "Auth:Admin";

    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FullName { get; set; } = "IconicU Admin";

    public bool IsConfigured => !string.IsNullOrWhiteSpace(Email) && !string.IsNullOrWhiteSpace(Password);
}

/// <summary>Creates the configured administrator on startup, or realigns it if the secret changed.</summary>
public class AdminAccountSeeder : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly AdminAccountOptions _options;
    private readonly ILogger<AdminAccountSeeder> _logger;

    public AdminAccountSeeder(
        IServiceScopeFactory scopeFactory,
        IOptions<AdminAccountOptions> options,
        ILogger<AdminAccountSeeder> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        if (!_options.IsConfigured)
        {
            var admins = await users.CountActiveAdminsAsync(cancellationToken);
            if (admins == 0)
            {
                _logger.LogWarning(
                    "No administrator exists and {Section}:Email / :Password are not configured — nobody can sign in. " +
                    "Set them with dotnet user-secrets and restart.",
                    AdminAccountOptions.SectionName);
            }
            return;
        }

        if (_options.Password.Length < CrmUser.MinPasswordLength)
        {
            _logger.LogError(
                "{Section}:Password is shorter than {Min} characters — the administrator was not created",
                AdminAccountOptions.SectionName, CrmUser.MinPasswordLength);
            return;
        }

        var admin = await users.FindByEmailAsync(_options.Email, cancellationToken);
        if (admin == null)
        {
            admin = CrmUser.Create(_options.Email, _options.FullName, hasher.Hash(_options.Password), UserRole.Admin);
            await users.AddAsync(admin, cancellationToken);
            _logger.LogInformation("Administrator {Email} created from configuration", admin.Email.Value);
            return;
        }

        // Keep the account usable even if the secret was rotated or the role changed in the database
        var changed = false;
        if (!hasher.Verify(_options.Password, admin.PasswordHash))
        {
            admin.ChangePassword(hasher.Hash(_options.Password));
            changed = true;
        }
        if (!admin.IsAdmin)
        {
            admin.ChangeRole(UserRole.Admin);
            changed = true;
        }
        if (!admin.IsActive)
        {
            admin.SetActive(true);
            changed = true;
        }

        if (changed)
        {
            await users.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Administrator {Email} realigned with configuration", admin.Email.Value);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
