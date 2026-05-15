using MediatR;
using PokerPlanning.Application.Abstractions.Persistence;
using PokerPlanning.Application.Abstractions.Time;
using PokerPlanning.Domain.Common;
using PokerPlanning.Domain.Participants;
using PokerPlanning.Domain.Rooms;

namespace PokerPlanning.Application.Features.RevealVotes;

public sealed class RevealVotesHandler(
    IRoomRepository rooms,
    IClock clock)
    : IRequestHandler<RevealVotesCommand, Result>
{
    public async Task<Result> Handle(RevealVotesCommand cmd, CancellationToken ct)
    {
        var room = await rooms.GetByIdAsync(new RoomId(cmd.RoomId), ct);
        if (room is null)
            return Result.Failure(RoomErrors.NotFound);

        var result = room.RevealVotes(new ParticipantId(cmd.CallerParticipantId), clock.UtcNow);
        if (result.IsFailure)
            return result;

        await rooms.SaveChangesAsync(ct);
        return Result.Success();
    }
}
