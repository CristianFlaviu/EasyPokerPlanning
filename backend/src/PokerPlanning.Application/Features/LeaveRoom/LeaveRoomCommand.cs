using MediatR;
using PokerPlanning.Domain.Common;

namespace PokerPlanning.Application.Features.LeaveRoom;

public sealed record LeaveRoomCommand(
    Guid RoomId,
    Guid ParticipantId) : IRequest<Result>;
