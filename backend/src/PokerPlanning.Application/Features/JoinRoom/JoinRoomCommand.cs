using MediatR;
using PokerPlanning.Domain.Common;
using PokerPlanning.Domain.Participants;

namespace PokerPlanning.Application.Features.JoinRoom;

public sealed record JoinRoomCommand(
    Guid RoomId,
    Guid ParticipantId,
    string DisplayName,
    ParticipantRole Role,
    string? Password,
    Guid? CallerUserId = null) : IRequest<Result<JoinRoomResult>>;

public sealed record JoinRoomResult(Guid RoomId, Guid ParticipantId);
