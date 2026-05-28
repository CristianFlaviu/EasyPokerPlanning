using PokerPlanning.Domain.Rooms;

namespace PokerPlanning.Application.Abstractions.Persistence;

public interface IRoomRepository
{
    Task<Room?> GetByIdAsync(RoomId id, CancellationToken ct);
    Task<Room?> GetByIdWithHistoryAsync(RoomId id, CancellationToken ct);
    Task<IReadOnlyList<Room>> ListByParticipantIdAsync(Guid participantId, Guid? userId, CancellationToken ct);
    Task AddAsync(Room room, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}
