using MediatR;
using PokerPlanning.Domain.Common;

namespace PokerPlanning.Application.Features.GetParticipantRooms;

public sealed record GetParticipantRoomsQuery(
    Guid ParticipantId,
    Guid? CallerUserId = null) : IRequest<Result<GetParticipantRoomsResult>>;

public sealed record GetParticipantRoomsResult(IReadOnlyList<ParticipantRoomSummaryResult> Rooms);

public sealed record ParticipantRoomSummaryResult(
    Guid Id,
    string Name,
    int CompletedRoundCount,
    DateTimeOffset LastActiveAt);
