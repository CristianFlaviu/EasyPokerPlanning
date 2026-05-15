using PokerPlanning.Domain.Common;
using PokerPlanning.Domain.Participants;

namespace PokerPlanning.Domain.Rooms.Events;

public sealed record VotesRevealedEvent(
    RoomId RoomId,
    Guid RoundId,
    IReadOnlyDictionary<ParticipantId, Card> Votes,
    DateTimeOffset OccurredAt) : IDomainEvent;
