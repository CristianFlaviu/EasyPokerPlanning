using MediatR;
using PokerPlanning.Application.Abstractions.LiveState;
using PokerPlanning.Application.Abstractions.Persistence;
using PokerPlanning.Application.Abstractions.Time;
using PokerPlanning.Domain.Common;
using PokerPlanning.Domain.Participants;
using PokerPlanning.Domain.Rooms;

namespace PokerPlanning.Application.Features.SubmitVote;

public sealed class SubmitVoteHandler(
    IRoomRepository rooms,
    IRoomLiveStateStore liveState,
    IClock clock)
    : IRequestHandler<SubmitVoteCommand, Result>
{
    public async Task<Result> Handle(SubmitVoteCommand cmd, CancellationToken ct)
    {
        var cardResult = Card.Create(cmd.Card);
        if (cardResult.IsFailure)
            return Result.Failure(cardResult.Error);

        var room = await rooms.GetByIdAsync(new RoomId(cmd.RoomId), ct);
        if (room is null)
            return Result.Failure(RoomErrors.NotFound);

        var result = room.SubmitVote(new ParticipantId(cmd.ParticipantId), cardResult.Value, clock.UtcNow);
        if (result.IsFailure)
            return result;

        await liveState.SaveCurrentRoundAsync(room.Id, room.CurrentRound!, ct);
        await rooms.SaveChangesAsync(ct);
        return Result.Success();
    }
}
