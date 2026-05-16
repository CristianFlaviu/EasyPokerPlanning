using Microsoft.EntityFrameworkCore;
using PokerPlanning.Application.Abstractions.Persistence;
using PokerPlanning.Domain.Participants;
using PokerPlanning.Domain.Rooms;

namespace PokerPlanning.Infrastructure.Persistence;

public sealed class RoomRepository(PokerPlanningDbContext db) : IRoomRepository
{
    public Task<Room?> GetByIdAsync(RoomId id, CancellationToken ct) =>
        db.Rooms
            .Include(r => r.Participants)
            .FirstOrDefaultAsync(r => r.Id == id, ct);

    public Task<Room?> GetByIdWithHistoryAsync(RoomId id, CancellationToken ct) =>
        db.Rooms
            .Include(r => r.Participants)
            .Include(r => r.History)
            .FirstOrDefaultAsync(r => r.Id == id, ct);

    public async Task<IReadOnlyList<Room>> ListByParticipantIdAsync(Guid participantId, CancellationToken ct)
    {
        var id = new ParticipantId(participantId);

        return
        await db.Rooms
            .Include(r => r.Participants)
            .Include(r => r.History)
            .Where(r => r.Participants.Any(p => p.Id == id))
            .ToListAsync(ct);
    }

    public async Task AddAsync(Room room, CancellationToken ct) =>
        await db.Rooms.AddAsync(room, ct);

    public Task SaveChangesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}
