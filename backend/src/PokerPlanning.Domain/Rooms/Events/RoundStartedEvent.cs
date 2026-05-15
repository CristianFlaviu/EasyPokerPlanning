using PokerPlanning.Domain.Common;

namespace PokerPlanning.Domain.Rooms.Events;

public sealed record RoundStartedEvent(
    RoomId RoomId,
    Guid RoundId,
    string? Title,
    DateTimeOffset OccurredAt) : IDomainEvent;
