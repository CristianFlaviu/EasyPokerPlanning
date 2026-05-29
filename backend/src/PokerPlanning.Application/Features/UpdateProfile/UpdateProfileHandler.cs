using MediatR;
using PokerPlanning.Application.Abstractions.Persistence;
using PokerPlanning.Application.Abstractions.Time;
using PokerPlanning.Application.Features.Users;
using PokerPlanning.Domain.Common;
using PokerPlanning.Domain.Users;

namespace PokerPlanning.Application.Features.UpdateProfile;

public sealed class UpdateProfileHandler(IUserRepository users, IClock clock)
    : IRequestHandler<UpdateProfileCommand, Result<UserDto>>
{
    public async Task<Result<UserDto>> Handle(UpdateProfileCommand cmd, CancellationToken ct)
    {
        var user = await users.GetByIdAsync(new UserId(cmd.UserId), ct);
        if (user is null)
            return Result.Failure<UserDto>(UserErrors.NotFound);

        var result = user.UpdateProfile(cmd.DisplayName, cmd.AvatarUrl, clock.UtcNow);
        if (result.IsFailure)
            return Result.Failure<UserDto>(result.Error);

        await users.SaveChangesAsync(ct);

        return Result.Success(new UserDto(user.Id.Value, user.Email, user.DisplayName, user.AvatarUrl));
    }
}
