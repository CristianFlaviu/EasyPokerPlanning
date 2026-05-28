using Microsoft.EntityFrameworkCore;
using PokerPlanning.Application.Abstractions.LiveState;
using PokerPlanning.Application.Abstractions.Persistence;
using PokerPlanning.Domain.Participants;
using PokerPlanning.Domain.Rooms;
using PokerPlanning.Domain.Users;

namespace PokerPlanning.Infrastructure.Persistence;

public sealed class RoomRepository(
    PokerPlanningDbContext db,
    IRoomLiveStateStore liveState) : IRoomRepository
{
    public async Task<Room?> GetByIdAsync(RoomId id, CancellationToken ct)
    {
        var room = await db.Rooms
            .Include(r => r.Participants)
            .FirstOrDefaultAsync(r => r.Id == id, ct);

        if (room is not null)
            room.RestoreCurrentRound(await liveState.GetCurrentRoundAsync(id, ct));

        return room;
    }

    public async Task<Room?> GetByIdWithHistoryAsync(RoomId id, CancellationToken ct)
    {
        var room = await db.Rooms
            .Include(r => r.Participants)
            .Include(r => r.History)
            .FirstOrDefaultAsync(r => r.Id == id, ct);

        if (room is not null)
            room.RestoreCurrentRound(await liveState.GetCurrentRoundAsync(id, ct));

        return room;
    }

    public async Task<IReadOnlyList<Room>> ListByParticipantIdAsync(Guid participantId, Guid? userId, CancellationToken ct)
    {
        var pid = new ParticipantId(participantId);
        UserId? uid = userId is { } id ? new UserId(id) : null;

        var rooms = await db.Rooms
            .Include(r => r.Participants)
            .Include(r => r.History)
            .ToListAsync(ct);

        return rooms
            .Where(r =>
                r.Participants.Any(p => p.Id == pid)
                || (uid is not null && r.Participants.Any(p => p.UserId == uid))
                || (uid is not null && r.OwnerUserId == uid)
                || r.History.Any(round => round.Votes.ContainsKey(pid)))
            .ToList();
    }

    public async Task AddAsync(Room room, CancellationToken ct) =>
        await db.Rooms.AddAsync(room, ct);

    public Task SaveChangesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}
