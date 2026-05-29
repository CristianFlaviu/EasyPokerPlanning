using PokerPlanning.Domain.Participants;
using PokerPlanning.Domain.Rooms;
using PokerPlanning.Domain.Users;

namespace PokerPlanning.Application.Abstractions.Security;

/// <summary>
/// Issues and validates server-signed per-room seat tokens. A token proves the
/// caller owns a specific participant seat in a specific room, so room actions are
/// authorized by the signed token rather than a client-supplied participant id.
/// </summary>
public interface IRoomAccessTokenService
{
    string Issue(RoomId roomId, ParticipantId participantId, UserId? userId = null);

    bool TryValidate(string? token, RoomId roomId, out RoomAccessToken access);
}

public sealed record RoomAccessToken(ParticipantId ParticipantId, UserId? UserId);
