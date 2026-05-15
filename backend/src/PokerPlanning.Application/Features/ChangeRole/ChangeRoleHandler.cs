using MediatR;
using PokerPlanning.Application.Abstractions.Persistence;
using PokerPlanning.Application.Abstractions.Time;
using PokerPlanning.Domain.Common;
using PokerPlanning.Domain.Participants;
using PokerPlanning.Domain.Rooms;

namespace PokerPlanning.Application.Features.ChangeRole;

public sealed class ChangeRoleHandler(
    IRoomRepository rooms,
    IClock clock)
    : IRequestHandler<ChangeRoleCommand, Result>
{
    public async Task<Result> Handle(ChangeRoleCommand cmd, CancellationToken ct)
    {
        var room = await rooms.GetByIdAsync(new RoomId(cmd.RoomId), ct);
        if (room is null)
            return Result.Failure(RoomErrors.NotFound);

        var result = room.ChangeRole(new ParticipantId(cmd.ParticipantId), cmd.Role, clock.UtcNow);
        if (result.IsFailure)
            return result;

        await rooms.SaveChangesAsync(ct);
        return Result.Success();
    }
}
