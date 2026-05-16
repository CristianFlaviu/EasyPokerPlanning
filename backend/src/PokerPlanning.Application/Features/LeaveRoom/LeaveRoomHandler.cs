using MediatR;
using PokerPlanning.Application.Abstractions.LiveState;
using PokerPlanning.Application.Abstractions.Persistence;
using PokerPlanning.Application.Abstractions.Time;
using PokerPlanning.Domain.Common;
using PokerPlanning.Domain.Participants;
using PokerPlanning.Domain.Rooms;

namespace PokerPlanning.Application.Features.LeaveRoom;

public sealed class LeaveRoomHandler(
    IRoomRepository rooms,
    IRoomLiveStateStore liveState,
    IClock clock)
    : IRequestHandler<LeaveRoomCommand, Result>
{
    public async Task<Result> Handle(LeaveRoomCommand cmd, CancellationToken ct)
    {
        var room = await rooms.GetByIdAsync(new RoomId(cmd.RoomId), ct);
        if (room is null)
            return Result.Failure(RoomErrors.NotFound);

        var result = room.LeaveRoom(new ParticipantId(cmd.ParticipantId), clock.UtcNow);
        if (result.IsFailure)
            return result;

        if (room.CurrentRound is not null)
            await liveState.SaveCurrentRoundAsync(room.Id, room.CurrentRound, ct);

        await rooms.SaveChangesAsync(ct);
        return Result.Success();
    }
}
