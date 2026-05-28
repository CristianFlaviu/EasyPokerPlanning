using MediatR;
using PokerPlanning.Application.Features.Users;
using PokerPlanning.Domain.Common;

namespace PokerPlanning.Application.Features.SignInWithGoogle;

public sealed record SignInWithGoogleCommand(
    string GoogleSubject,
    string Email,
    string Name,
    string? Picture) : IRequest<Result<UserDto>>;
