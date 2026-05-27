using System.Text.Json;
using PokerPlanning.Application.Abstractions.LiveState;
using PokerPlanning.Domain.Participants;
using PokerPlanning.Domain.Rooms;
using StackExchange.Redis;

namespace PokerPlanning.Infrastructure.LiveState;

public sealed class RedisRoomLiveStateStore(IConnectionMultiplexer redis) : IRoomLiveStateStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly IDatabase _db = redis.GetDatabase();

    public async Task<Round?> GetCurrentRoundAsync(RoomId roomId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var value = await _db.StringGetAsync(CurrentRoundKey(roomId));
        if (!value.HasValue)
            return null;

        var dto = JsonSerializer.Deserialize<CurrentRoundDto>(value.ToString(), JsonOptions);
        if (dto is null)
            return null;

        if (!Enum.TryParse<RoundPhase>(dto.Phase, ignoreCase: true, out var phase))
            throw new InvalidOperationException($"Redis room state has an invalid round phase: {dto.Phase}.");

        var votes = dto.Votes.ToDictionary(
            vote => new ParticipantId(vote.Key),
            vote => Card.Create(vote.Value).Value);

        var round = Round.Restore(dto.Id, dto.Title, phase, dto.StartedAt, votes);
        if (round.IsFailure)
            throw new InvalidOperationException(round.Error.Message);

        return round.Value;
    }

    public async Task SaveCurrentRoundAsync(RoomId roomId, Round round, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var dto = new CurrentRoundDto(
            round.Id,
            round.Title,
            round.Phase.ToString(),
            round.StartedAt,
            round.Votes.ToDictionary(vote => vote.Key.Value, vote => vote.Value.Value));

        var json = JsonSerializer.Serialize(dto, JsonOptions);
        await _db.StringSetAsync(CurrentRoundKey(roomId), json);
    }

    public async Task ClearCurrentRoundAsync(RoomId roomId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await _db.KeyDeleteAsync(CurrentRoundKey(roomId));
    }

    public async Task TrackConnectionAsync(
        RoomId roomId,
        ParticipantId participantId,
        string connectionId,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var existing = await GetConnectionAsync(connectionId);
        if (existing?.RoomId == roomId.Value && existing.ParticipantId == participantId.Value)
        {
            await _db.SetAddAsync(RoomConnectionsKey(roomId), connectionId);
            await _db.SetAddAsync(ParticipantConnectionsKey(roomId, participantId), connectionId);
            return;
        }

        if (existing is not null)
            await RemoveConnectionAsync(connectionId, ct);

        var connection = new ConnectionDto(roomId.Value, participantId.Value);
        var json = JsonSerializer.Serialize(connection, JsonOptions);

        await _db.StringSetAsync(ConnectionKey(connectionId), json);
        await _db.SetAddAsync(RoomConnectionsKey(roomId), connectionId);
        await _db.SetAddAsync(ParticipantConnectionsKey(roomId, participantId), connectionId);
    }

    public async Task<IReadOnlyList<string>> GetParticipantConnectionIdsAsync(
        RoomId roomId,
        ParticipantId participantId,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var members = await _db.SetMembersAsync(ParticipantConnectionsKey(roomId, participantId));
        return members
            .Where(member => member.HasValue)
            .Select(member => member.ToString())
            .ToList();
    }

    public async Task RemoveConnectionAsync(string connectionId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var connection = await GetConnectionAsync(connectionId);
        if (connection is null)
            return;

        var roomId = new RoomId(connection.RoomId);
        var participantId = new ParticipantId(connection.ParticipantId);

        await _db.KeyDeleteAsync(ConnectionKey(connectionId));
        await _db.SetRemoveAsync(RoomConnectionsKey(roomId), connectionId);
        await _db.SetRemoveAsync(ParticipantConnectionsKey(roomId, participantId), connectionId);

        if (await _db.SetLengthAsync(ParticipantConnectionsKey(roomId, participantId)) == 0)
            await _db.KeyDeleteAsync(ParticipantConnectionsKey(roomId, participantId));

        if (await _db.SetLengthAsync(RoomConnectionsKey(roomId)) == 0)
        {
            await _db.KeyDeleteAsync(RoomConnectionsKey(roomId));
        }
    }

    private async Task<ConnectionDto?> GetConnectionAsync(string connectionId)
    {
        var value = await _db.StringGetAsync(ConnectionKey(connectionId));
        return value.HasValue
            ? JsonSerializer.Deserialize<ConnectionDto>(value.ToString(), JsonOptions)
            : null;
    }

    private static RedisKey CurrentRoundKey(RoomId roomId) => $"room:{roomId.Value}:current-round";
    private static RedisKey RoomConnectionsKey(RoomId roomId) => $"room:{roomId.Value}:connections";
    private static RedisKey ParticipantConnectionsKey(RoomId roomId, ParticipantId participantId) =>
        $"room:{roomId.Value}:participant:{participantId.Value}:connections";
    private static RedisKey ConnectionKey(string connectionId) => $"connection:{connectionId}";

    private sealed record CurrentRoundDto(
        Guid Id,
        string? Title,
        string Phase,
        DateTimeOffset StartedAt,
        Dictionary<Guid, string> Votes);

    private sealed record ConnectionDto(Guid RoomId, Guid ParticipantId);
}
