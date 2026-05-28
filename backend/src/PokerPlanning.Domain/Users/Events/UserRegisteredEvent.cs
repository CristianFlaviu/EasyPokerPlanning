using PokerPlanning.Domain.Common;

namespace PokerPlanning.Domain.Users.Events;

public sealed record UserRegisteredEvent(
    UserId UserId,
    string Email,
    string Provider,
    DateTimeOffset OccurredAt) : IDomainEvent;
