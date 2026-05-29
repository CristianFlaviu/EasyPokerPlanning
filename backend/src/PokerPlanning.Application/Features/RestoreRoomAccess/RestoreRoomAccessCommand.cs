using MediatR;
using PokerPlanning.Domain.Common;

namespace PokerPlanning.Application.Features.RestoreRoomAccess;

public sealed record RestoreRoomAccessCommand(
    Guid RoomId,
    Guid CallerUserId) : IRequest<Result<RestoreRoomAccessResult>>;

public sealed record RestoreRoomAccessResult(
    Guid RoomId,
    Guid ParticipantId,
    string AccessToken);
