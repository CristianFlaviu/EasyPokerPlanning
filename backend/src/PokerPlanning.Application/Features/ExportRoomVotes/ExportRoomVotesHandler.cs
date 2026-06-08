using MediatR;
using PokerPlanning.Application.Abstractions.Persistence;
using PokerPlanning.Domain.Common;
using PokerPlanning.Domain.Participants;
using PokerPlanning.Domain.Rooms;
using PokerPlanning.Domain.Users;

namespace PokerPlanning.Application.Features.ExportRoomVotes;

public sealed class ExportRoomVotesHandler(IRoomRepository rooms)
    : IRequestHandler<ExportRoomVotesQuery, Result<ExportRoomVotesResult>>
{
    public async Task<Result<ExportRoomVotesResult>> Handle(ExportRoomVotesQuery query, CancellationToken ct)
    {
        var room = await rooms.GetByIdWithHistoryAsync(new RoomId(query.RoomId), ct);
        if (room is null)
            return Result.Failure<ExportRoomVotesResult>(RoomErrors.NotFound);

        var hasUserAccess = query.CallerUserId is { } callerUserId
            && room.HasUserAccess(new UserId(callerUserId));

        if (!hasUserAccess)
            return Result.Failure<ExportRoomVotesResult>(RoomErrors.NotAuthorized);

        var displayNames = room.Participants.ToDictionary(p => p.Id, p => p.DisplayName);

        var rounds = room.History
            .OrderBy(r => r.EndedAt)
            .Select((r, index) => new ExportRoundResult(
                index + 1,
                r.Title,
                r.FinalEstimate?.Value,
                r.Votes.ToDictionary(v => v.Key.Value, v => v.Value.Value)))
            .ToList();

        // Voter columns = current participants first (join order), then anyone who only appears in
        // history (already left), in first-vote order. This keeps every vote in the file.
        var voterIds = new List<ParticipantId>();
        var seen = new HashSet<ParticipantId>();
        foreach (var participant in room.Participants)
            if (seen.Add(participant.Id))
                voterIds.Add(participant.Id);
        foreach (var completed in room.History.OrderBy(r => r.EndedAt))
            foreach (var voterId in completed.Votes.Keys)
                if (seen.Add(voterId))
                    voterIds.Add(voterId);

        var voters = BuildVoterColumns(voterIds, displayNames);

        return Result.Success(new ExportRoomVotesResult(room.Id.Value, room.Name, voters, rounds));
    }

    // Resolves a header name per voter, disambiguating duplicate display names with a short id suffix.
    private static IReadOnlyList<ExportVoter> BuildVoterColumns(
        IReadOnlyList<ParticipantId> voterIds,
        IReadOnlyDictionary<ParticipantId, string> displayNames)
    {
        var rawNames = voterIds.ToDictionary(
            id => id,
            id => displayNames.TryGetValue(id, out var name) ? name : id.Value.ToString());

        var duplicated = rawNames.Values
            .GroupBy(name => name, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return voterIds
            .Select(id =>
            {
                var name = rawNames[id];
                if (duplicated.Contains(name))
                    name = $"{name} ({id.Value.ToString()[..8]})";
                return new ExportVoter(id.Value, name);
            })
            .ToList();
    }
}
