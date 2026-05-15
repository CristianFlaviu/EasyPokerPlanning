using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PokerPlanning.Application.Abstractions.Persistence;
using PokerPlanning.Application.Abstractions.Security;
using PokerPlanning.Application.Abstractions.Time;
using PokerPlanning.Infrastructure.Persistence;
using PokerPlanning.Infrastructure.Security;
using PokerPlanning.Infrastructure.Time;

namespace PokerPlanning.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<IRoomRepository, RoomRepository>();
        services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();
        services.AddSingleton<IClock, SystemClock>();
        return services;
    }
}
