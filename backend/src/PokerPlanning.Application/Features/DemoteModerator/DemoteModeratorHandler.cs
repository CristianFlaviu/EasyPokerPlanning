using MediatR;
using PokerPlanning.Application.Abstractions.Persistence;
using PokerPlanning.Application.Abstractions.Time;
using PokerPlanning.Domain.Common;
using PokerPlanning.Domain.Participants;
using PokerPlanning.Domain.Rooms;

namespace PokerPlanning.Application.Features.DemoteModerator;

public sealed class DemoteModeratorHandler(
    IRoomRepository rooms,
    IClock clock)
    : IRequestHandler<DemoteModeratorCommand, Result>
{
    public async Task<Result> Handle(DemoteModeratorCommand cmd, CancellationToken ct)
    {
        var room = await rooms.GetByIdAsync(new RoomId(cmd.RoomId), ct);
        if (room is null)
            return Result.Failure(RoomErrors.NotFound);

        var result = room.DemoteModerator(
            new ParticipantId(cmd.CallerParticipantId),
            new ParticipantId(cmd.ParticipantId),
            clock.UtcNow);

        if (result.IsFailure)
            return result;

        await rooms.SaveChangesAsync(ct);
        return Result.Success();
    }
}
