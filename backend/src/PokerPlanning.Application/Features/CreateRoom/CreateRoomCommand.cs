using MediatR;
using PokerPlanning.Domain.Common;

namespace PokerPlanning.Application.Features.CreateRoom;

public sealed record CreateRoomCommand(
    string Name,
    string? Password,
    Guid OwnerParticipantId,
    string OwnerDisplayName,
    Guid? OwnerUserId = null) : IRequest<Result<CreateRoomResult>>;

public sealed record CreateRoomResult(Guid RoomId, Guid OwnerParticipantId);
