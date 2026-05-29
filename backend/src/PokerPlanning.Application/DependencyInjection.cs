using System.Reflection;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using PokerPlanning.Application.Abstractions.Security;
using PokerPlanning.Application.Behaviors;
using PokerPlanning.Application.Security;

namespace PokerPlanning.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services, string? mediatRLicenseKey = null)
    {
        var assembly = Assembly.GetExecutingAssembly();

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);
            if (!string.IsNullOrWhiteSpace(mediatRLicenseKey))
            {
                cfg.LicenseKey = mediatRLicenseKey;
            }
        });
        services.AddValidatorsFromAssembly(assembly, includeInternalTypes: true);
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddScoped<IRoomAccessAuthorizer, RoomAccessAuthorizer>();

        return services;
    }
}
