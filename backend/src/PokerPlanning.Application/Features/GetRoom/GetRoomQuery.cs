using MediatR;
using PokerPlanning.Domain.Common;

namespace PokerPlanning.Application.Features.GetRoom;

public sealed record GetRoomQuery(Guid RoomId, Guid CallerParticipantId) : IRequest<Result<GetRoomResult>>;

public sealed record GetRoomResult(
    Guid Id,
    string Name,
    Guid OwnerId,
    bool IsPasswordProtected,
    IReadOnlyList<GetRoomParticipantResult> Participants,
    IReadOnlyList<Guid> ModeratorIds,
    GetRoomRoundResult? CurrentRound);

public sealed record GetRoomParticipantResult(
    Guid Id,
    string DisplayName,
    string Role,
    string? AvatarUrl);

public sealed record GetRoomRoundResult(
    Guid Id,
    string? Title,
    string Phase,
    IReadOnlyList<GetRoomVoteResult> Votes);

public sealed record GetRoomVoteResult(
    Guid ParticipantId,
    string? Card,
    bool IsRevealed);
