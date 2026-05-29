using PokerPlanning.Domain.Common;

namespace PokerPlanning.Domain.Users.Events;

public sealed record UserProfileUpdatedEvent(
    UserId UserId,
    string DisplayName,
    string? AvatarUrl,
    DateTimeOffset OccurredAt) : IDomainEvent;
