using PokerPlanning.Domain.Rooms;
using PokerPlanning.Domain.Participants;

namespace PokerPlanning.Application.Abstractions.LiveState;

public interface IRoomLiveStateStore
{
    Task<Round?> GetCurrentRoundAsync(RoomId roomId, CancellationToken ct);
    Task SaveCurrentRoundAsync(RoomId roomId, Round round, CancellationToken ct);
    Task ClearCurrentRoundAsync(RoomId roomId, CancellationToken ct);
    Task TrackConnectionAsync(RoomId roomId, ParticipantId participantId, string connectionId, CancellationToken ct);
    Task RemoveConnectionAsync(string connectionId, CancellationToken ct);
}
