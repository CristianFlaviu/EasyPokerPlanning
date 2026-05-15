using MediatR;
using PokerPlanning.Application.Abstractions.Persistence;
using PokerPlanning.Application.Abstractions.Time;
using PokerPlanning.Domain.Common;
using PokerPlanning.Domain.Participants;
using PokerPlanning.Domain.Rooms;

namespace PokerPlanning.Application.Features.EndRound;

public sealed class EndRoundHandler(
    IRoomRepository rooms,
    IClock clock)
    : IRequestHandler<EndRoundCommand, Result>
{
    public async Task<Result> Handle(EndRoundCommand cmd, CancellationToken ct)
    {
        Card? finalEstimate = null;
        if (!string.IsNullOrWhiteSpace(cmd.FinalEstimate))
        {
            var cardResult = Card.Create(cmd.FinalEstimate);
            if (cardResult.IsFailure)
                return Result.Failure(cardResult.Error);

            finalEstimate = cardResult.Value;
        }

        var room = await rooms.GetByIdAsync(new RoomId(cmd.RoomId), ct);
        if (room is null)
            return Result.Failure(RoomErrors.NotFound);

        var result = room.EndRound(new ParticipantId(cmd.CallerParticipantId), finalEstimate, clock.UtcNow);
        if (result.IsFailure)
            return result;

        await rooms.SaveChangesAsync(ct);
        return Result.Success();
    }
}
