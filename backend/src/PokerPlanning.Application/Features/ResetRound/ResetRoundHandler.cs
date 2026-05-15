using MediatR;
using PokerPlanning.Application.Abstractions.Persistence;
using PokerPlanning.Application.Abstractions.Time;
using PokerPlanning.Domain.Common;
using PokerPlanning.Domain.Participants;
using PokerPlanning.Domain.Rooms;

namespace PokerPlanning.Application.Features.ResetRound;

public sealed class ResetRoundHandler(
    IRoomRepository rooms,
    IClock clock)
    : IRequestHandler<ResetRoundCommand, Result>
{
    public async Task<Result> Handle(ResetRoundCommand cmd, CancellationToken ct)
    {
        var room = await rooms.GetByIdAsync(new RoomId(cmd.RoomId), ct);
        if (room is null)
            return Result.Failure(RoomErrors.NotFound);

        var result = room.ResetCurrentRound(new ParticipantId(cmd.CallerParticipantId), clock.UtcNow);
        if (result.IsFailure)
            return result;

        await rooms.SaveChangesAsync(ct);
        return Result.Success();
    }
}
