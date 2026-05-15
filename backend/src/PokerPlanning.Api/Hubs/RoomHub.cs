using Microsoft.AspNetCore.SignalR;

namespace PokerPlanning.Api.Hubs;

public sealed class RoomHub : Hub<IRoomClient>
{
    public async Task JoinRoomGroup(Guid roomId)
    {
        _ = ResolveParticipantId();
        await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(roomId));
    }

    public async Task LeaveRoomGroup(Guid roomId)
    {
        _ = ResolveParticipantId();
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(roomId));
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
