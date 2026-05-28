using PokerPlanning.Domain.Common;
using PokerPlanning.Domain.Participants;
using PokerPlanning.Domain.Users;

namespace PokerPlanning.Domain.Rooms.Events;

public sealed record ParticipantJoinedEvent(
    RoomId RoomId,
    ParticipantId ParticipantId,
    string DisplayName,
    ParticipantRole Role,
    UserId? UserId,
    DateTimeOffset OccurredAt) : IDomainEvent;
