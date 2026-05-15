using PokerPlanning.Domain.Common;
using PokerPlanning.Domain.Participants;

namespace PokerPlanning.Domain.Rooms.Events;

public sealed record ParticipantRoleChangedEvent(
    RoomId RoomId,
    ParticipantId ParticipantId,
    ParticipantRole Role,
    DateTimeOffset OccurredAt) : IDomainEvent;
