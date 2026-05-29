using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;
using PokerPlanning.Application.Abstractions.Security;
using PokerPlanning.Domain.Participants;
using PokerPlanning.Domain.Rooms;

namespace PokerPlanning.Infrastructure.Security;

/// <summary>
/// Stateless seat token: <c>base64url(payload).base64url(HMAC-SHA256(payload))</c>
/// where payload is <c>roomId:participantId:issuedUnixSeconds</c>. No server-side
/// storage; integrity and binding to (room, participant) are guaranteed by the HMAC.
/// </summary>
public sealed class HmacRoomAccessTokenService : IRoomAccessTokenService
{
    private static readonly TimeSpan Lifetime = TimeSpan.FromDays(30);

    private readonly byte[] _key;

    public HmacRoomAccessTokenService(string secret)
    {
        if (string.IsNullOrWhiteSpace(secret))
            throw new ArgumentException("Room access token secret must be configured.", nameof(secret));

        _key = Encoding.UTF8.GetBytes(secret);
    }

    public string Issue(RoomId roomId, ParticipantId participantId)
    {
        var issued = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var payload = Encoding.UTF8.GetBytes($"{roomId.Value:N}:{participantId.Value:N}:{issued}");
        var signature = HMACSHA256.HashData(_key, payload);
        return $"{Base64Url.EncodeToString(payload)}.{Base64Url.EncodeToString(signature)}";
    }

    public bool TryValidate(string? token, RoomId roomId, out ParticipantId participantId)
    {
        participantId = default;

        if (string.IsNullOrWhiteSpace(token))
            return false;

        var dot = token.IndexOf('.');
        if (dot <= 0 || dot == token.Length - 1)
            return false;

        byte[] payload;
        byte[] signature;
        try
        {
            payload = Base64Url.DecodeFromChars(token.AsSpan(0, dot));
            signature = Base64Url.DecodeFromChars(token.AsSpan(dot + 1));
        }
        catch (FormatException)
        {
            return false;
        }

        var expected = HMACSHA256.HashData(_key, payload);
        if (!CryptographicOperations.FixedTimeEquals(signature, expected))
            return false;

        var parts = Encoding.UTF8.GetString(payload).Split(':');
        if (parts.Length != 3)
            return false;

        if (!Guid.TryParseExact(parts[0], "N", out var tokenRoomId) || tokenRoomId != roomId.Value)
            return false;

        if (!Guid.TryParseExact(parts[1], "N", out var seatId))
            return false;

        if (!long.TryParse(parts[2], out var issuedUnix))
            return false;

        if (DateTimeOffset.FromUnixTimeSeconds(issuedUnix) + Lifetime < DateTimeOffset.UtcNow)
            return false;

        participantId = new ParticipantId(seatId);
        return true;
    }
}
