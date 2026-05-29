using Microsoft.AspNetCore.SignalR;
using PokerPlanning.Application.Abstractions.LiveState;
using PokerPlanning.Application.Abstractions.Security;
using PokerPlanning.Domain.Participants;
using PokerPlanning.Domain.Rooms;

namespace PokerPlanning.Api.Hubs;

public sealed class RoomHub(
    IRoomLiveStateStore liveState,
    IRoomAccessTokenService tokens,
    IRoomAccessAuthorizer access)
    : Hub<IRoomClient>
{
    public async Task JoinRoomGroup(Guid roomId)
    {
        var participantId = ResolveSeat(roomId);
        if (!await access.IsCurrentParticipantAsync(new RoomId(roomId), participantId, Context.ConnectionAborted))
            throw new HubException("A current room seat is required.");

        await liveState.TrackConnectionAsync(
            new RoomId(roomId),
            participantId,
            Context.ConnectionId,
            Context.ConnectionAborted);

        await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(roomId));
    }

    public async Task LeaveRoomGroup(Guid roomId)
    {
        await liveState.RemoveConnectionAsync(Context.ConnectionId, Context.ConnectionAborted);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(roomId));
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await liveState.RemoveConnectionAsync(Context.ConnectionId, CancellationToken.None);
        await base.OnDisconnectedAsync(exception);
    }

    public static string GroupName(Guid roomId) => $"room:{roomId}";

    // Listening to a room's live events requires a valid seat token for that room,
    // so non-joined callers cannot subscribe to a room they have not accessed.
    private ParticipantId ResolveSeat(Guid roomId)
    {
        var http = Context.GetHttpContext();

        // SignalR's accessTokenFactory delivers the token as the access_token query
        // string (WebSockets) or Authorization: Bearer header (other transports).
        var token = http?.Request.Query["access_token"].ToString();
        if (string.IsNullOrWhiteSpace(token))
            token = http?.Request.Headers["X-Room-Token"].ToString();

        if (tokens.TryValidate(token, new RoomId(roomId), out var participantId))
            return participantId;

        throw new HubException("A valid room access token is required.");
    }
}
