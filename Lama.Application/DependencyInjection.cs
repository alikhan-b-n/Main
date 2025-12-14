using Lama.Application.Common;
using Lama.Application.CustomerManagement.Commands;
using Lama.Application.CustomerService.Commands;
using Lama.Application.SalesManagement.Commands;
using Microsoft.Extensions.DependencyInjection;

namespace Lama.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Register command handlers
        services.AddScoped<ICommandHandler<CreateAccountCommand, Guid>, CreateAccountCommandHandler>();
        services.AddScoped<ICommandHandler<CreateContactCommand, Guid>, CreateContactCommandHandler>();
        services.AddScoped<ICommandHandler<CreateOpportunityCommand, Guid>, CreateOpportunityCommandHandler>();
        services.AddScoped<ICommandHandler<CreateSupportCaseCommand, Guid>, CreateSupportCaseCommandHandler>();

        return services;
    }
}
