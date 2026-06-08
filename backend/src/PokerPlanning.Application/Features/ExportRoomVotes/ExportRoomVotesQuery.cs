using MediatR;
using PokerPlanning.Domain.Common;

namespace PokerPlanning.Application.Features.ExportRoomVotes;

public sealed record ExportRoomVotesQuery(
    Guid RoomId,
    Guid? CallerUserId = null) : IRequest<Result<ExportRoomVotesResult>>;

public sealed record ExportRoomVotesResult(
    Guid RoomId,
    string RoomName,
    IReadOnlyList<ExportVoter> Voters,
    IReadOnlyList<ExportRoundResult> Rounds);

// One column per voter. ParticipantId keys the per-round lookup; Name is the (de-duplicated) header.
public sealed record ExportVoter(
    Guid ParticipantId,
    string Name);

public sealed record ExportRoundResult(
    int Number,
    string? Title,
    string? FinalEstimate,
    IReadOnlyDictionary<Guid, string> VotesByParticipant);
