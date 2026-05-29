using MediatR;
using PokerPlanning.Domain.Common;

namespace PokerPlanning.Application.Features.GetRoomHistory;

public sealed record GetRoomHistoryQuery(
    Guid RoomId,
    Guid? CallerParticipantId = null,
    Guid? CallerUserId = null) : IRequest<Result<GetRoomHistoryResult>>;

public sealed record GetRoomHistoryResult(
    Guid RoomId,
    IReadOnlyList<CompletedRoundResult> Rounds);

public sealed record CompletedRoundResult(
    Guid Id,
    string? Title,
    IReadOnlyList<CompletedVoteResult> Votes,
    string? FinalEstimate,
    DateTimeOffset StartedAt,
    DateTimeOffset EndedAt);

public sealed record CompletedVoteResult(
    Guid ParticipantId,
    string Card);
