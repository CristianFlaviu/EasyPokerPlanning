using PokerPlanning.Domain.Common;
using PokerPlanning.Domain.Participants;

namespace PokerPlanning.Domain.Rooms.Events;

public sealed record ModeratorDemotedEvent(
    RoomId RoomId,
    ParticipantId ParticipantId,
    DateTimeOffset OccurredAt) : IDomainEvent;
