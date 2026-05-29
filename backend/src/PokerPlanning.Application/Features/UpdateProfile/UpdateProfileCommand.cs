using MediatR;
using PokerPlanning.Application.Features.Users;
using PokerPlanning.Domain.Common;

namespace PokerPlanning.Application.Features.UpdateProfile;

public sealed record UpdateProfileCommand(
    Guid UserId,
    string DisplayName,
    string? AvatarUrl) : IRequest<Result<UserDto>>;
