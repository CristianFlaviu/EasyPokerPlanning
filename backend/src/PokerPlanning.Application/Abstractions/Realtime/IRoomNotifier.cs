using PokerPlanning.Domain.Participants;
using PokerPlanning.Domain.Rooms;

namespace PokerPlanning.Application.Abstractions.Realtime;

public interface IRoomNotifier
{
    Task ParticipantJoinedAsync(
        RoomId roomId,
        ParticipantId participantId,
        string displayName,
        ParticipantRole role,
        CancellationToken ct);

    Task RoundStartedAsync(RoomId roomId, Guid roundId, string? title, CancellationToken ct);

    Task VoteSubmittedAsync(RoomId roomId, Guid roundId, ParticipantId participantId, CancellationToken ct);

    Task VotesRevealedAsync(
        RoomId roomId,
        Guid roundId,
        IReadOnlyDictionary<ParticipantId, Card> votes,
        CancellationToken ct);

    Task RoundResetAsync(RoomId roomId, Guid roundId, CancellationToken ct);

    Task RoundEndedAsync(RoomId roomId, Guid roundId, Card? finalEstimate, CancellationToken ct);

    Task ModeratorPromotedAsync(RoomId roomId, ParticipantId participantId, CancellationToken ct);

    Task ModeratorDemotedAsync(RoomId roomId, ParticipantId participantId, CancellationToken ct);

    Task ParticipantRoleChangedAsync(
        RoomId roomId,
        ParticipantId participantId,
        ParticipantRole role,
        CancellationToken ct);
}
