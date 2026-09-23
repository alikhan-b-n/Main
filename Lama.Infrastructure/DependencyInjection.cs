using Lama.Application.Common;
using Lama.Application.AccessControl;
using Lama.Application.LeadManagement;
using Lama.Infrastructure.Persistence;
using Lama.Infrastructure.Repositories;
using Lama.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Lama.Integrations.AI.Clients;
using Lama.Integrations.AI.Configuration;
using Lama.Integrations.AI.Interfaces;
using Lama.Integrations.AI.Services;

namespace Lama.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure Npgsql to handle DateTime with Kind=Unspecified as UTC
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

        // Register DbContext with PostgreSQL
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                npgsqlOptions => npgsqlOptions.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)
            )
        );

        // Register repositories as scoped for EF Core
        services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>));
        services.AddScoped<ILeadRepository, LeadRepository>();

        // Access control: users, password hashing and the administrator from configuration
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.Configure<AdminAccountOptions>(configuration.GetSection(AdminAccountOptions.SectionName));
        services.AddHostedService<AdminAccountSeeder>();

        // AI provider defaults from appsettings.json. API keys are NEVER
        // configured here — they always come from the request payload.
        services.Configure<OllamaSettings>(configuration.GetSection(OllamaSettings.SectionName));
        services.Configure<LmStudioSettings>(configuration.GetSection(LmStudioSettings.SectionName));
        services.Configure<GroqSettings>(configuration.GetSection(GroqSettings.SectionName));

        // Named HttpClients used by the completion clients.
        services.AddHttpClient(OllamaCompletionClient.HttpClientName);
        services.AddHttpClient(OpenAiCompatibleCompletionClient.HttpClientName);

        // Provider-level clients (registered as singletons — they hold no
        // request-scoped state; HttpClient lifetimes are managed by
        // IHttpClientFactory).
        services.AddSingleton<OllamaCompletionClient>();
        services.AddSingleton<OpenAiCompatibleCompletionClient>();
        services.AddSingleton<IAiCompletionClientFactory, AiCompletionClientFactory>();

        // Scenario-level service (provider-agnostic — routes through the factory).
        services.AddScoped<ITextAiService, TextAiService>();

        return services;
    }
}
