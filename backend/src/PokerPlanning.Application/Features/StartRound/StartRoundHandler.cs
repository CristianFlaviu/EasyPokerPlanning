using MediatR;
using PokerPlanning.Application.Abstractions.LiveState;
using PokerPlanning.Application.Abstractions.Persistence;
using PokerPlanning.Application.Abstractions.Time;
using PokerPlanning.Domain.Common;
using PokerPlanning.Domain.Participants;
using PokerPlanning.Domain.Rooms;

namespace PokerPlanning.Application.Features.StartRound;

public sealed class StartRoundHandler(
    IRoomRepository rooms,
    IRoomLiveStateStore liveState,
    IClock clock)
    : IRequestHandler<StartRoundCommand, Result<StartRoundResult>>
{
    public async Task<Result<StartRoundResult>> Handle(StartRoundCommand cmd, CancellationToken ct)
    {
        var room = await rooms.GetByIdAsync(new RoomId(cmd.RoomId), ct);
        if (room is null)
            return Result.Failure<StartRoundResult>(RoomErrors.NotFound);

        var result = room.StartRound(new ParticipantId(cmd.CallerParticipantId), cmd.Title, clock.UtcNow);
        if (result.IsFailure)
            return Result.Failure<StartRoundResult>(result.Error);

        await liveState.SaveCurrentRoundAsync(room.Id, room.CurrentRound!, ct);
        await rooms.SaveChangesAsync(ct);
        return Result.Success(new StartRoundResult(room.Id.Value, room.CurrentRound!.Id));
    }
}
