using Microsoft.AspNetCore.SignalR;
using PokerPlanning.Application.Abstractions.LiveState;
using PokerPlanning.Domain.Participants;
using PokerPlanning.Domain.Rooms;

namespace PokerPlanning.Api.Hubs;

public sealed class RoomHub(IRoomLiveStateStore liveState) : Hub<IRoomClient>
{
    public async Task JoinRoomGroup(Guid roomId)
    {
        var participantId = ResolveParticipantId();
        await liveState.TrackConnectionAsync(
            new RoomId(roomId),
            new ParticipantId(participantId),
            Context.ConnectionId,
            Context.ConnectionAborted);

        await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(roomId));
    }

    public async Task LeaveRoomGroup(Guid roomId)
    {
        _ = ResolveParticipantId();
        await liveState.RemoveConnectionAsync(Context.ConnectionId, Context.ConnectionAborted);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(roomId));
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await liveState.RemoveConnectionAsync(Context.ConnectionId, CancellationToken.None);
        await base.OnDisconnectedAsync(exception);
    }

    public static string GroupName(Guid roomId) => $"room:{roomId}";

    private Guid ResolveParticipantId()
    {
        var http = Context.GetHttpContext();
        var raw = http?.Request.Headers["X-Participant-Id"].ToString();

        if (string.IsNullOrWhiteSpace(raw))
            raw = http?.Request.Query["participantId"].ToString();

        if (Guid.TryParse(raw, out var participantId) && participantId != Guid.Empty)
            return participantId;

        throw new HubException("Participant id is required.");
    }
}
