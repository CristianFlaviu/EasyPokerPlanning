using PokerPlanning.Application.Abstractions.Persistence;
using PokerPlanning.Application.Abstractions.Security;
using PokerPlanning.Domain.Participants;
using PokerPlanning.Domain.Rooms;

namespace PokerPlanning.Application.Security;

public sealed class RoomAccessAuthorizer(IRoomRepository rooms) : IRoomAccessAuthorizer
{
    public async Task<bool> IsCurrentParticipantAsync(
        RoomId roomId,
        ParticipantId participantId,
        CancellationToken ct)
    {
        var room = await rooms.GetByIdAsync(roomId, ct);
        return room?.HasParticipant(participantId) == true;
    }
}
