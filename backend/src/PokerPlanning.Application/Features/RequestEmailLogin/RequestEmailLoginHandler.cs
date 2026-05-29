using System.Security.Cryptography;
using System.Text;
using MediatR;
using PokerPlanning.Application.Abstractions.Email;
using PokerPlanning.Application.Abstractions.Persistence;
using PokerPlanning.Application.Abstractions.Time;
using PokerPlanning.Domain.Common;
using PokerPlanning.Domain.Users;

namespace PokerPlanning.Application.Features.RequestEmailLogin;

public sealed class RequestEmailLoginHandler(
    IUserRepository users,
    IEmailLoginTokenRepository tokens,
    IEmailSender emailSender,
    IClock clock)
    : IRequestHandler<RequestEmailLoginCommand, Result>
{
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromMinutes(15);

    public async Task<Result> Handle(RequestEmailLoginCommand cmd, CancellationToken ct)
    {
        EmailLoginModes.TryParse(cmd.Mode, out var mode);

        var normalizedEmail = User.NormalizeEmail(cmd.Email);
        var existing = await users.GetByEmailAsync(normalizedEmail, ct);
        if (mode == EmailLoginMode.Login && existing is null)
            return Result.Success();

        var rawToken = CreateToken();
        var tokenHash = HashToken(rawToken);
        var createResult = EmailLoginToken.Create(
            tokenHash,
            normalizedEmail,
            cmd.DisplayName,
            mode,
            cmd.ReturnUrl,
            clock.UtcNow,
            TokenLifetime);

        if (createResult.IsFailure)
            return Result.Failure(createResult.Error);

        await tokens.AddAsync(createResult.Value, ct);
        await tokens.SaveChangesAsync(ct);

        var displayName = existing?.DisplayName ?? cmd.DisplayName ?? normalizedEmail;
        var loginUrl = BuildCallbackUrl(cmd.CallbackBaseUrl, rawToken);
        await emailSender.SendMagicLinkAsync(
            normalizedEmail,
            displayName,
            loginUrl,
            createResult.Value.ExpiresAt,
            ct);

        return Result.Success();
    }

    internal static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static string CreateToken()
    {
        Span<byte> bytes = stackalloc byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static string BuildCallbackUrl(string callbackBaseUrl, string token)
    {
        var separator = callbackBaseUrl.Contains('?') ? '&' : '?';
        return $"{callbackBaseUrl}{separator}token={Uri.EscapeDataString(token)}";
    }
}
