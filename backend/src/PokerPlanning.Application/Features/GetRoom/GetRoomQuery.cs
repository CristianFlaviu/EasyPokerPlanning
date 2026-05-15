using MediatR;
using PokerPlanning.Domain.Common;

namespace PokerPlanning.Application.Features.GetRoom;

public sealed record GetRoomQuery(Guid RoomId) : IRequest<Result<GetRoomResult>>;

public sealed record GetRoomResult(
    Guid Id,
    string Name,
    Guid OwnerId,
    bool IsPasswordProtected,
    IReadOnlyList<GetRoomParticipantResult> Participants);

public sealed record GetRoomParticipantResult(
    Guid Id,
    string DisplayName,
    string Role);
