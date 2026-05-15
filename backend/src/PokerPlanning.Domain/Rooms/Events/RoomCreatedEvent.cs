using PokerPlanning.Domain.Common;
using PokerPlanning.Domain.Participants;

namespace PokerPlanning.Domain.Rooms.Events;

public sealed record RoomCreatedEvent(
    RoomId RoomId,
    ParticipantId OwnerId,
    DateTimeOffset OccurredAt) : IDomainEvent;
