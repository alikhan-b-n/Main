using Lama.Application.Common;
using Lama.Infrastructure.Persistence;
using Lama.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
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

        // Configure AI Integration
        // Ollama settings from appsettings.json
        services.Configure<OllamaSettings>(configuration.GetSection(OllamaSettings.SectionName));

        // Register HttpClient for Ollama with typed client
        services.AddHttpClient<ITextAiService, OllamaTextAiService>();

        // Note: OllamaTextAiService is an AI client library
        // Business logic (commands/queries/handlers) are in Application layer
        // Falls back to simple heuristic if Ollama is not running

        return services;
    }
}
