using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PokerPlanning.Application.Abstractions.Email;
using PokerPlanning.Application.Abstractions.LiveState;
using PokerPlanning.Application.Abstractions.Persistence;
using PokerPlanning.Application.Abstractions.Security;
using PokerPlanning.Application.Abstractions.Time;
using PokerPlanning.Infrastructure.Email;
using PokerPlanning.Infrastructure.LiveState;
using PokerPlanning.Infrastructure.Persistence;
using PokerPlanning.Infrastructure.Security;
using PokerPlanning.Infrastructure.Storage;
using PokerPlanning.Infrastructure.Time;
using PokerPlanning.Application.Abstractions.Storage;

namespace PokerPlanning.Infrastructure;

public static class DependencyInjection
{
    // Development-only fallback. Production (Fly) must set RoomAccessToken:Secret.
    private const string DevRoomAccessTokenSecret =
        "dev-only-room-access-token-secret-change-me-in-production";

    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string? roomAccessTokenSecret = null)
    {
        services.AddScoped<IRoomRepository, RoomRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IEmailLoginTokenRepository, EmailLoginTokenRepository>();
        services.AddScoped<IFeedbackRepository, FeedbackRepository>();
        services.AddScoped<IEmailSender, GmailSmtpEmailSender>();
        services.AddScoped<IAvatarStorage, AzureBlobAvatarStorage>();
        services.AddSingleton<IRoomLiveStateStore, RedisRoomLiveStateStore>();
        services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IRoomAccessTokenService>(
            new HmacRoomAccessTokenService(
                string.IsNullOrWhiteSpace(roomAccessTokenSecret)
                    ? DevRoomAccessTokenSecret
                    : roomAccessTokenSecret));
        return services;
    }
}
