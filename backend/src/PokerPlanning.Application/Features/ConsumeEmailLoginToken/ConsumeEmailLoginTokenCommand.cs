using MediatR;
using PokerPlanning.Application.Features.Users;
using PokerPlanning.Domain.Common;

namespace PokerPlanning.Application.Features.ConsumeEmailLoginToken;

public sealed record ConsumeEmailLoginTokenCommand(string Token) : IRequest<Result<ConsumeEmailLoginTokenResult>>;

public sealed record ConsumeEmailLoginTokenResult(UserDto User, string ReturnUrl);
