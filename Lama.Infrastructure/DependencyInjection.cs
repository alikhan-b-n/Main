using Lama.Application.Common;
using Lama.Infrastructure.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace Lama.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        // Register repositories as singletons for in-memory storage
        services.AddSingleton(typeof(IRepository<>), typeof(InMemoryRepository<>));

        return services;
    }
}
