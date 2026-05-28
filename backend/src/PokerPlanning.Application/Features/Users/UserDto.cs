namespace PokerPlanning.Application.Features.Users;

public sealed record UserDto(
    Guid Id,
    string Email,
    string DisplayName,
    string? AvatarUrl);
