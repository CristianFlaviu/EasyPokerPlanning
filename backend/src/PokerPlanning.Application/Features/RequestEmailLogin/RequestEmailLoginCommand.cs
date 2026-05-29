using MediatR;
using PokerPlanning.Domain.Common;

namespace PokerPlanning.Application.Features.RequestEmailLogin;

public sealed record RequestEmailLoginCommand(
    string Mode,
    string Email,
    string? DisplayName,
    string ReturnUrl,
    string CallbackBaseUrl) : IRequest<Result>;
