using PokerPlanning.Domain.Participants;
using PokerPlanning.Domain.Rooms;

namespace PokerPlanning.Application.Abstractions.Security;

public interface IRoomAccessAuthorizer
{
    Task<bool> IsCurrentParticipantAsync(RoomId roomId, ParticipantId participantId, CancellationToken ct);
}
