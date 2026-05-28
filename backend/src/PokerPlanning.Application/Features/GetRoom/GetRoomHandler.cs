using MediatR;
using PokerPlanning.Application.Abstractions.Persistence;
using PokerPlanning.Domain.Common;
using PokerPlanning.Domain.Rooms;
using PokerPlanning.Domain.Users;

namespace PokerPlanning.Application.Features.GetRoom;

public sealed class GetRoomHandler(IRoomRepository rooms, IUserRepository users)
    : IRequestHandler<GetRoomQuery, Result<GetRoomResult>>
{
    public async Task<Result<GetRoomResult>> Handle(GetRoomQuery query, CancellationToken ct)
    {
        var room = await rooms.GetByIdAsync(new RoomId(query.RoomId), ct);
        if (room is null)
            return Result.Failure<GetRoomResult>(RoomErrors.NotFound);

        var avatars = new Dictionary<UserId, string?>();
        var userIds = room.Participants
            .Where(p => p.UserId is not null)
            .Select(p => p.UserId!.Value)
            .Distinct()
            .ToList();
        foreach (var userId in userIds)
        {
            var user = await users.GetByIdAsync(userId, ct);
            if (user is not null)
                avatars[userId] = user.AvatarUrl;
        }

        var participants = room.Participants
            .Select(p => new GetRoomParticipantResult(
                p.Id.Value,
                p.DisplayName,
                p.Role.ToString(),
                p.UserId is { } uid && avatars.TryGetValue(uid, out var avatar) ? avatar : null))
            .ToList();

        GetRoomRoundResult? currentRound = null;
        if (room.CurrentRound is not null)
        {
            var isRevealed = room.CurrentRound.Phase == RoundPhase.Revealed;
            var callerCanSeeOwnVote = query.CallerParticipantId != Guid.Empty;
            currentRound = new GetRoomRoundResult(
                room.CurrentRound.Id,
                room.CurrentRound.Title,
                room.CurrentRound.Phase.ToString(),
                room.CurrentRound.Votes
                    .Select(v => new GetRoomVoteResult(
                        v.Key.Value,
                        isRevealed || (callerCanSeeOwnVote && v.Key.Value == query.CallerParticipantId)
                            ? v.Value.Value
                            : null,
                        isRevealed))
                    .ToList());
        }

        return Result.Success(new GetRoomResult(
            room.Id.Value,
            room.Name,
            room.OwnerId.Value,
            room.IsPasswordProtected,
            participants,
            room.ModeratorIds.Select(id => id.Value).ToList(),
            currentRound));
    }
}
