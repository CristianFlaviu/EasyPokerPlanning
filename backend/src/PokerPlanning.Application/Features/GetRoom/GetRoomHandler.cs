using MediatR;
using PokerPlanning.Application.Abstractions.Persistence;
using PokerPlanning.Domain.Common;
using PokerPlanning.Domain.Rooms;

namespace PokerPlanning.Application.Features.GetRoom;

public sealed class GetRoomHandler(IRoomRepository rooms)
    : IRequestHandler<GetRoomQuery, Result<GetRoomResult>>
{
    public async Task<Result<GetRoomResult>> Handle(GetRoomQuery query, CancellationToken ct)
    {
        var room = await rooms.GetByIdAsync(new RoomId(query.RoomId), ct);
        if (room is null)
            return Result.Failure<GetRoomResult>(RoomErrors.NotFound);

        var participants = room.Participants
            .Select(p => new GetRoomParticipantResult(
                p.Id.Value,
                p.DisplayName,
                p.Role.ToString()))
            .ToList();

        return Result.Success(new GetRoomResult(
            room.Id.Value,
            room.Name,
            room.OwnerId.Value,
            room.IsPasswordProtected,
            participants));
    }
}
