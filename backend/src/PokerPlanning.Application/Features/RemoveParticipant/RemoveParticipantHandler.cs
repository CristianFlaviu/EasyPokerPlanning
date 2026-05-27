using MediatR;
using PokerPlanning.Application.Abstractions.LiveState;
using PokerPlanning.Application.Abstractions.Persistence;
using PokerPlanning.Application.Abstractions.Time;
using PokerPlanning.Domain.Common;
using PokerPlanning.Domain.Participants;
using PokerPlanning.Domain.Rooms;

namespace PokerPlanning.Application.Features.RemoveParticipant;

public sealed class RemoveParticipantHandler(
    IRoomRepository rooms,
    IRoomLiveStateStore liveState,
    IClock clock)
    : IRequestHandler<RemoveParticipantCommand, Result>
{
    public async Task<Result> Handle(RemoveParticipantCommand cmd, CancellationToken ct)
    {
        var room = await rooms.GetByIdAsync(new RoomId(cmd.RoomId), ct);
        if (room is null)
            return Result.Failure(RoomErrors.NotFound);

        var result = room.RemoveParticipant(
            new ParticipantId(cmd.CallerParticipantId),
            new ParticipantId(cmd.ParticipantId),
            clock.UtcNow);

        if (result.IsFailure)
            return result;

        if (room.CurrentRound is not null)
            await liveState.SaveCurrentRoundAsync(room.Id, room.CurrentRound, ct);

        await rooms.SaveChangesAsync(ct);
        return Result.Success();
    }
}
