using Microsoft.AspNetCore.SignalR;
using PokerPlanning.Api.Hubs;
using PokerPlanning.Application.Abstractions.LiveState;
using PokerPlanning.Application.Abstractions.Realtime;
using PokerPlanning.Domain.Participants;
using PokerPlanning.Domain.Rooms;

namespace PokerPlanning.Api.Realtime;

public sealed class RoomNotifier(
    IHubContext<RoomHub, IRoomClient> hubContext,
    IRoomLiveStateStore liveState) : IRoomNotifier
{
    public Task ParticipantJoinedAsync(
        RoomId roomId,
        ParticipantId participantId,
        string displayName,
        ParticipantRole role,
        CancellationToken ct)
    {
        var message = new ParticipantJoinedMessage(
            participantId.Value,
            displayName,
            role.ToString());

        return hubContext.Clients
            .Group(RoomHub.GroupName(roomId.Value))
            .ParticipantJoined(message);
    }

    public async Task ParticipantLeftAsync(RoomId roomId, ParticipantId participantId, CancellationToken ct)
    {
        var message = new ParticipantLeftMessage(participantId.Value);
        var groupName = RoomHub.GroupName(roomId.Value);

        await hubContext.Clients
            .Group(groupName)
            .ParticipantLeft(message);

        var connectionIds = await liveState.GetParticipantConnectionIdsAsync(roomId, participantId, ct);
        foreach (var connectionId in connectionIds)
        {
            await hubContext.Groups.RemoveFromGroupAsync(connectionId, groupName, ct);
            await liveState.RemoveConnectionAsync(connectionId, ct);
        }
    }

    public Task RoundStartedAsync(RoomId roomId, Guid roundId, string? title, CancellationToken ct)
    {
        var message = new RoundStartedMessage(roundId, title, "Voting");

        return hubContext.Clients
            .Group(RoomHub.GroupName(roomId.Value))
            .RoundStarted(message);
    }

    public Task VoteSubmittedAsync(
        RoomId roomId,
        Guid roundId,
        ParticipantId participantId,
        CancellationToken ct)
    {
        var message = new VoteSubmittedMessage(roundId, participantId.Value);

        return hubContext.Clients
            .Group(RoomHub.GroupName(roomId.Value))
            .VoteSubmitted(message);
    }

    public Task VotesRevealedAsync(
        RoomId roomId,
        Guid roundId,
        IReadOnlyDictionary<ParticipantId, Card> votes,
        CancellationToken ct)
    {
        var message = new VotesRevealedMessage(
            roundId,
            votes.Select(v => new RevealedVoteMessage(v.Key.Value, v.Value.Value)).ToList());

        return hubContext.Clients
            .Group(RoomHub.GroupName(roomId.Value))
            .VotesRevealed(message);
    }

    public Task RoundResetAsync(RoomId roomId, Guid roundId, CancellationToken ct)
    {
        var message = new RoundResetMessage(roundId);

        return hubContext.Clients
            .Group(RoomHub.GroupName(roomId.Value))
            .RoundReset(message);
    }

    public Task RoundEndedAsync(RoomId roomId, Guid roundId, Card? finalEstimate, CancellationToken ct)
    {
        var message = new RoundEndedMessage(roundId, finalEstimate?.Value);

        return hubContext.Clients
            .Group(RoomHub.GroupName(roomId.Value))
            .RoundEnded(message);
    }

    public Task ModeratorPromotedAsync(RoomId roomId, ParticipantId participantId, CancellationToken ct)
    {
        var message = new ModeratorChangedMessage(participantId.Value);

        return hubContext.Clients
            .Group(RoomHub.GroupName(roomId.Value))
            .ModeratorPromoted(message);
    }

    public Task ModeratorDemotedAsync(RoomId roomId, ParticipantId participantId, CancellationToken ct)
    {
        var message = new ModeratorChangedMessage(participantId.Value);

        return hubContext.Clients
            .Group(RoomHub.GroupName(roomId.Value))
            .ModeratorDemoted(message);
    }

    public Task ParticipantRoleChangedAsync(
        RoomId roomId,
        ParticipantId participantId,
        ParticipantRole role,
        CancellationToken ct)
    {
        var message = new ParticipantRoleChangedMessage(participantId.Value, role.ToString());

        return hubContext.Clients
            .Group(RoomHub.GroupName(roomId.Value))
            .ParticipantRoleChanged(message);
    }
}
