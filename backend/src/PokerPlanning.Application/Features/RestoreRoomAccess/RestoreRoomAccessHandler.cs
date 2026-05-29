using MediatR;
using PokerPlanning.Application.Abstractions.Persistence;
using PokerPlanning.Application.Abstractions.Security;
using PokerPlanning.Domain.Common;
using PokerPlanning.Domain.Rooms;
using PokerPlanning.Domain.Users;

namespace PokerPlanning.Application.Features.RestoreRoomAccess;

public sealed class RestoreRoomAccessHandler(
    IRoomRepository rooms,
    IRoomAccessTokenService accessTokens)
    : IRequestHandler<RestoreRoomAccessCommand, Result<RestoreRoomAccessResult>>
{
    public async Task<Result<RestoreRoomAccessResult>> Handle(
        RestoreRoomAccessCommand cmd,
        CancellationToken ct)
    {
        var room = await rooms.GetByIdAsync(new RoomId(cmd.RoomId), ct);
        if (room is null)
            return Result.Failure<RestoreRoomAccessResult>(RoomErrors.NotFound);

        var userId = new UserId(cmd.CallerUserId);
        var participantId = room.GetParticipantIdForUser(userId);
        if (participantId is null)
            return Result.Failure<RestoreRoomAccessResult>(RoomErrors.NotAuthorized);

        var token = accessTokens.Issue(room.Id, participantId.Value, userId);
        return Result.Success(new RestoreRoomAccessResult(room.Id.Value, participantId.Value.Value, token));
    }
}
