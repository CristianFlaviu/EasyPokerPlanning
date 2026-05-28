using MediatR;
using PokerPlanning.Application.Abstractions.Persistence;
using PokerPlanning.Domain.Common;

namespace PokerPlanning.Application.Features.GetParticipantRooms;

public sealed class GetParticipantRoomsHandler(IRoomRepository rooms)
    : IRequestHandler<GetParticipantRoomsQuery, Result<GetParticipantRoomsResult>>
{
    public async Task<Result<GetParticipantRoomsResult>> Handle(GetParticipantRoomsQuery query, CancellationToken ct)
    {
        var participantRooms = await rooms.ListByParticipantIdAsync(query.ParticipantId, query.CallerUserId, ct);

        var summaries = participantRooms
            .Select(room => new ParticipantRoomSummaryResult(
                room.Id.Value,
                room.Name,
                room.History.Count,
                room.History.Count == 0 ? room.CreatedAt : room.History.Max(r => r.EndedAt)))
            .OrderByDescending(room => room.LastActiveAt)
            .ToList();

        return Result.Success(new GetParticipantRoomsResult(summaries));
    }
}
