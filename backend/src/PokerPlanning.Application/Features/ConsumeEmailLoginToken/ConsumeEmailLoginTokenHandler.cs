using MediatR;
using PokerPlanning.Application.Abstractions.Persistence;
using PokerPlanning.Application.Abstractions.Time;
using PokerPlanning.Application.Features.RequestEmailLogin;
using PokerPlanning.Application.Features.Users;
using PokerPlanning.Domain.Common;
using PokerPlanning.Domain.Users;

namespace PokerPlanning.Application.Features.ConsumeEmailLoginToken;

public sealed class ConsumeEmailLoginTokenHandler(
    IUserRepository users,
    IEmailLoginTokenRepository tokens,
    IClock clock)
    : IRequestHandler<ConsumeEmailLoginTokenCommand, Result<ConsumeEmailLoginTokenResult>>
{
    public async Task<Result<ConsumeEmailLoginTokenResult>> Handle(
        ConsumeEmailLoginTokenCommand cmd,
        CancellationToken ct)
    {
        var tokenHash = RequestEmailLoginHandler.HashToken(cmd.Token);
        var token = await tokens.GetByTokenHashAsync(tokenHash, ct);
        if (token is null)
            return Result.Failure<ConsumeEmailLoginTokenResult>(EmailLoginTokenErrors.InvalidToken);

        var consumeResult = token.Consume(clock.UtcNow);
        if (consumeResult.IsFailure)
            return Result.Failure<ConsumeEmailLoginTokenResult>(consumeResult.Error);

        var emailLogin = new ExternalLogin(ExternalLogin.EmailProvider, token.Email);
        var user = await users.GetByExternalLoginAsync(ExternalLogin.EmailProvider, token.Email, ct)
            ?? await users.GetByEmailAsync(token.Email, ct);

        if (user is null)
        {
            if (token.Mode == EmailLoginMode.Login)
                return Result.Failure<ConsumeEmailLoginTokenResult>(EmailLoginTokenErrors.InvalidToken);

            var createResult = User.Create(
                token.Email,
                token.DisplayName ?? DisplayNameFromEmail(token.Email),
                avatarUrl: null,
                emailLogin,
                clock.UtcNow);

            if (createResult.IsFailure)
                return Result.Failure<ConsumeEmailLoginTokenResult>(createResult.Error);

            user = createResult.Value;
            await users.AddAsync(user, ct);
        }
        else
        {
            var linkResult = user.LinkExternalLogin(emailLogin);
            if (linkResult.IsFailure)
                return Result.Failure<ConsumeEmailLoginTokenResult>(linkResult.Error);

            user.RecordLogin(clock.UtcNow);
        }

        await users.SaveChangesAsync(ct);
        var dto = new UserDto(user.Id.Value, user.Email, user.DisplayName, user.AvatarUrl);
        return Result.Success(new ConsumeEmailLoginTokenResult(dto, token.ReturnUrl));
    }

    private static string DisplayNameFromEmail(string email)
    {
        var atIndex = email.IndexOf('@', StringComparison.Ordinal);
        var candidate = atIndex > 0 ? email[..atIndex] : email;
        candidate = candidate.Trim();

        if (candidate.Length > User.MaxDisplayNameLength)
            return candidate[..User.MaxDisplayNameLength];

        return string.IsNullOrWhiteSpace(candidate) ? email : candidate;
    }
}
