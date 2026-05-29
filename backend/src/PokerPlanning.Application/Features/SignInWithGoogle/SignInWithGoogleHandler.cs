using MediatR;
using PokerPlanning.Application.Abstractions.Persistence;
using PokerPlanning.Application.Abstractions.Time;
using PokerPlanning.Application.Features.Users;
using PokerPlanning.Domain.Common;
using PokerPlanning.Domain.Users;

namespace PokerPlanning.Application.Features.SignInWithGoogle;

public sealed class SignInWithGoogleHandler(IUserRepository users, IClock clock)
    : IRequestHandler<SignInWithGoogleCommand, Result<UserDto>>
{
    public async Task<Result<UserDto>> Handle(SignInWithGoogleCommand cmd, CancellationToken ct)
    {
        var now = clock.UtcNow;

        var existing = await users.GetByExternalLoginAsync(
            ExternalLogin.GoogleProvider,
            cmd.GoogleSubject,
            ct);

        if (existing is not null)
        {
            var updateResult = existing.UpdateProfile(cmd.Name, cmd.Picture, now);
            if (updateResult.IsFailure)
                return Result.Failure<UserDto>(updateResult.Error);

            existing.RecordLogin(now);
            await users.SaveChangesAsync(ct);

            return Result.Success(ToDto(existing));
        }

        var sameEmailUser = await users.GetByEmailAsync(cmd.Email, ct);
        if (sameEmailUser is not null)
        {
            var linkResult = sameEmailUser.LinkExternalLogin(
                new ExternalLogin(ExternalLogin.GoogleProvider, cmd.GoogleSubject));
            if (linkResult.IsFailure)
                return Result.Failure<UserDto>(linkResult.Error);

            var updateResult = sameEmailUser.UpdateProfile(cmd.Name, cmd.Picture, now);
            if (updateResult.IsFailure)
                return Result.Failure<UserDto>(updateResult.Error);

            sameEmailUser.RecordLogin(now);
            await users.SaveChangesAsync(ct);
            return Result.Success(ToDto(sameEmailUser));
        }

        var createResult = User.Create(
            cmd.Email,
            cmd.Name,
            cmd.Picture,
            new ExternalLogin(ExternalLogin.GoogleProvider, cmd.GoogleSubject),
            now);

        if (createResult.IsFailure)
            return Result.Failure<UserDto>(createResult.Error);

        var user = createResult.Value;
        await users.AddAsync(user, ct);
        await users.SaveChangesAsync(ct);

        return Result.Success(ToDto(user));
    }

    private static UserDto ToDto(User user) =>
        new(user.Id.Value, user.Email, user.DisplayName, user.AvatarUrl);
}
