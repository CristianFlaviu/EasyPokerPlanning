using PokerPlanning.Domain.Common;

namespace PokerPlanning.Domain.Rooms.Events;

public sealed record RoundEndedEvent(
    RoomId RoomId,
    Guid RoundId,
    Card? FinalEstimate,
    DateTimeOffset OccurredAt) : IDomainEvent;
