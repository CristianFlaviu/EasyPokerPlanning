using PokerPlanning.Domain.Common;
using PokerPlanning.Domain.Participants;

namespace PokerPlanning.Domain.Rooms.Events;

public sealed record VoteSubmittedEvent(
    RoomId RoomId,
    Guid RoundId,
    ParticipantId ParticipantId,
    DateTimeOffset OccurredAt) : IDomainEvent;
