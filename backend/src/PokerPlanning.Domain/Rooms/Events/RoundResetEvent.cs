using PokerPlanning.Domain.Common;

namespace PokerPlanning.Domain.Rooms.Events;

public sealed record RoundResetEvent(
    RoomId RoomId,
    Guid RoundId,
    DateTimeOffset OccurredAt) : IDomainEvent;
