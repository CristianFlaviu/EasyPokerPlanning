using MediatR;
using PokerPlanning.Application.Features.Users;
using PokerPlanning.Domain.Common;

namespace PokerPlanning.Application.Features.GetCurrentUser;

public sealed record GetCurrentUserQuery(Guid UserId) : IRequest<Result<UserDto>>;
