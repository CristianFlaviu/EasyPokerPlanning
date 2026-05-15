using MediatR;
using PokerPlanning.Application.Abstractions.Persistence;
using PokerPlanning.Domain.Common;
using PokerPlanning.Domain.Rooms;

namespace PokerPlanning.Application.Features.GetRoomHistory;

public sealed class GetRoomHistoryHandler(IRoomRepository rooms)
    : IRequestHandler<GetRoomHistoryQuery, Result<GetRoomHistoryResult>>
{
    public async Task<Result<GetRoomHistoryResult>> Handle(GetRoomHistoryQuery query, CancellationToken ct)
    {
        var room = await rooms.GetByIdWithHistoryAsync(new RoomId(query.RoomId), ct);
        if (room is null)
            return Result.Failure<GetRoomHistoryResult>(RoomErrors.NotFound);

        var completedRounds = room.History
            .OrderByDescending(r => r.EndedAt)
            .Select(r => new CompletedRoundResult(
                r.Id,
                r.Title,
                r.Votes
                    .Select(v => new CompletedVoteResult(v.Key.Value, v.Value.Value))
                    .ToList(),
                r.FinalEstimate?.Value,
                r.StartedAt,
                r.EndedAt))
            .ToList();

        return Result.Success(new GetRoomHistoryResult(room.Id.Value, completedRounds));
    }
}
