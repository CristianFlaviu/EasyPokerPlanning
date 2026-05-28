using MediatR;
using PokerPlanning.Application.Abstractions.Persistence;
using PokerPlanning.Application.Features.Users;
using PokerPlanning.Domain.Common;
using PokerPlanning.Domain.Users;

namespace PokerPlanning.Application.Features.GetCurrentUser;

public sealed class GetCurrentUserHandler(IUserRepository users)
    : IRequestHandler<GetCurrentUserQuery, Result<UserDto>>
{
    public async Task<Result<UserDto>> Handle(GetCurrentUserQuery query, CancellationToken ct)
    {
        var user = await users.GetByIdAsync(new UserId(query.UserId), ct);
        if (user is null)
            return Result.Failure<UserDto>(UserErrors.NotFound);

        return Result.Success(new UserDto(user.Id.Value, user.Email, user.DisplayName, user.AvatarUrl));
    }
}
