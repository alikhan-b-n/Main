using System.Reflection;
using FluentValidation;
using Lama.Application.Common.Behaviors;
using Lama.Integrations.AI.Commands;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Lama.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Register MediatR and automatically discover all handlers in this assembly and AI integration assembly
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
            cfg.RegisterServicesFromAssemblyContaining<SummarizeActivityCommand>();

            // Register pipeline behaviors in order of execution
            cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
            cfg.AddOpenBehavior(typeof(PerformanceBehavior<,>));
        });

        // Register all validators from this assembly for FluentValidation
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        return services;
    }
}
