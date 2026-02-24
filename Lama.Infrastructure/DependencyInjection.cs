using Lama.Application.Common;
using Lama.Infrastructure.Persistence;
using Lama.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Lama.Integrations.AI.Interfaces;
using Lama.Integrations.AI.Services;
using Lama.Infrastructure.AI;

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

        // Register AI text service (local fallback)
        services.AddScoped<ITextAiService, LocalTextAiService>();

        // Register AI activity repository adapter
        services.AddScoped<IActivityRepository, ActivityRepositoryAdapter>();

        return services;
    }
}
