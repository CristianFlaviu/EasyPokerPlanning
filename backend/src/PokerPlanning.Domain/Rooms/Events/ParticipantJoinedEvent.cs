using PokerPlanning.Domain.Common;
using PokerPlanning.Domain.Participants;

namespace PokerPlanning.Domain.Rooms.Events;

public sealed record ParticipantJoinedEvent(
    RoomId RoomId,
    ParticipantId ParticipantId,
    DateTimeOffset OccurredAt) : IDomainEvent;
