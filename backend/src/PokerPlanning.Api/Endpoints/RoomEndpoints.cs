using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using PokerPlanning.Api.Common;
using PokerPlanning.Application.Features.ChangeRole;
using PokerPlanning.Application.Features.DemoteModerator;
using PokerPlanning.Application.Features.CreateRoom;
using PokerPlanning.Application.Features.EndRound;
using PokerPlanning.Application.Features.GetRoom;
using PokerPlanning.Application.Features.GetParticipantRooms;
using PokerPlanning.Application.Features.GetRoomHistory;
using PokerPlanning.Application.Features.JoinRoom;
using PokerPlanning.Application.Features.LeaveRoom;
using PokerPlanning.Application.Features.PromoteModerator;
using PokerPlanning.Application.Features.RemoveParticipant;
using PokerPlanning.Application.Features.ResetRound;
using PokerPlanning.Application.Features.RevealVotes;
using PokerPlanning.Application.Features.StartRound;
using PokerPlanning.Application.Features.SubmitVote;
using PokerPlanning.Domain.Participants;

namespace PokerPlanning.Api.Endpoints;

public static class RoomEndpoints
{
    public static IEndpointRouteBuilder MapRoomEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/rooms").WithTags("Rooms");

        group.MapPost("", CreateRoom)
            .WithName("CreateRoom")
            .WithSummary("Create a new poker planning room.");

        group.MapGet("{id:guid}", GetRoom)
            .WithName("GetRoom")
            .WithSummary("Get room metadata and participants.");

        group.MapGet("history", GetParticipantRooms)
            .WithName("GetParticipantRooms")
            .WithSummary("Get room history summaries for a participant.");

        group.MapGet("{id:guid}/history", GetRoomHistory)
            .WithName("GetRoomHistory")
            .WithSummary("Get completed rounds for a room.");

        group.MapPost("{id:guid}/join", JoinRoom)
            .WithName("JoinRoom")
            .WithSummary("Join an existing poker planning room.");

        group.MapPost("{id:guid}/rounds", StartRound)
            .WithName("StartRound")
            .WithSummary("Start a voting round.");

        group.MapPost("{id:guid}/round/vote", SubmitVote)
            .WithName("SubmitVote")
            .WithSummary("Submit or replace the caller's vote.");

        group.MapPost("{id:guid}/round/reveal", RevealVotes)
            .WithName("RevealVotes")
            .WithSummary("Reveal votes for the current round.");

        group.MapPost("{id:guid}/round/reset", ResetRound)
            .WithName("ResetRound")
            .WithSummary("Reset votes for the current round.");

        group.MapPost("{id:guid}/round/end", EndRound)
            .WithName("EndRound")
            .WithSummary("End the current round and archive it to history.");

        group.MapPost("{id:guid}/moderators/{participantId:guid}", PromoteModerator)
            .WithName("PromoteModerator")
            .WithSummary("Promote a participant to moderator.");

        group.MapDelete("{id:guid}/moderators/{participantId:guid}", DemoteModerator)
            .WithName("DemoteModerator")
            .WithSummary("Demote a participant from moderator.");

        group.MapPost("{id:guid}/participants/me/role", ChangeRole)
            .WithName("ChangeRole")
            .WithSummary("Change the caller's voter/observer role.");

        group.MapDelete("{id:guid}/participants/{participantId:guid}", RemoveParticipant)
            .WithName("RemoveParticipant")
            .WithSummary("Remove a participant from the room.");

        group.MapDelete("{id:guid}/participants/me", LeaveRoom)
            .WithName("LeaveRoom")
            .WithSummary("Leave the room as the caller.");

        return app;
    }

    private static async Task<IResult> CreateRoom(
        CreateRoomRequest request,
        IMediator mediator,
        HttpContext http,
        CancellationToken ct)
    {
        var participantId = ResolveParticipantId(http, request.OwnerParticipantId);

        var command = new CreateRoomCommand(
            request.Name,
            request.Password,
            participantId,
            request.OwnerDisplayName);

        var result = await mediator.Send(command, ct);

        return result.ToHttpResult(value =>
            TypedResults.Created($"/rooms/{value.RoomId}", new CreateRoomResponse(value.RoomId, value.OwnerParticipantId)));
    }

    private static Guid ResolveParticipantId(HttpContext http, Guid? fromBody)
    {
        if (fromBody is { } id && id != Guid.Empty)
            return id;

        if (http.Request.Headers.TryGetValue("X-Participant-Id", out var header)
            && Guid.TryParse(header.ToString(), out var headerId))
            return headerId;

        return Guid.Empty;
    }

    private static async Task<IResult> GetRoom(
        Guid id,
        IMediator mediator,
        HttpContext http,
        CancellationToken ct)
    {
        var participantId = ResolveParticipantId(http, null);
        var result = await mediator.Send(new GetRoomQuery(id, participantId), ct);

        return result.ToHttpResult(value =>
            TypedResults.Ok(new GetRoomResponse(
                value.Id,
                value.Name,
                value.OwnerId,
                value.IsPasswordProtected,
                value.Participants
                    .Select(p => new RoomParticipantResponse(p.Id, p.DisplayName, p.Role))
                    .ToList(),
                value.ModeratorIds,
                value.CurrentRound is null
                    ? null
                    : new CurrentRoundResponse(
                        value.CurrentRound.Id,
                        value.CurrentRound.Title,
                        value.CurrentRound.Phase,
                        value.CurrentRound.Votes
                            .Select(v => new VoteResponse(v.ParticipantId, v.Card, v.IsRevealed))
                            .ToList()))));
    }

    private static async Task<IResult> JoinRoom(
        Guid id,
        JoinRoomRequest request,
        IMediator mediator,
        HttpContext http,
        CancellationToken ct)
    {
        var participantId = ResolveParticipantId(http, request.ParticipantId);
        var role = Enum.TryParse<ParticipantRole>(request.Role, ignoreCase: true, out var parsedRole)
            ? parsedRole
            : (ParticipantRole)(-1);

        var command = new JoinRoomCommand(
            id,
            participantId,
            request.DisplayName,
            role,
            request.Password);

        var result = await mediator.Send(command, ct);

        return result.ToHttpResult(value =>
            TypedResults.Ok(new JoinRoomResponse(value.RoomId, value.ParticipantId)));
    }

    private static async Task<IResult> GetParticipantRooms(
        Guid? participantId,
        IMediator mediator,
        HttpContext http,
        CancellationToken ct)
    {
        var resolvedParticipantId = ResolveParticipantId(http, participantId);
        var result = await mediator.Send(new GetParticipantRoomsQuery(resolvedParticipantId), ct);

        return result.ToHttpResult(value =>
            TypedResults.Ok(new GetParticipantRoomsResponse(
                value.Rooms
                    .Select(room => new ParticipantRoomSummaryResponse(
                        room.Id,
                        room.Name,
                        room.CompletedRoundCount,
                        room.LastActiveAt))
                    .ToList())));
    }

    private static async Task<IResult> GetRoomHistory(
        Guid id,
        IMediator mediator,
        CancellationToken ct)
    {
        var result = await mediator.Send(new GetRoomHistoryQuery(id), ct);

        return result.ToHttpResult(value =>
            TypedResults.Ok(new RoomHistoryResponse(
                value.RoomId,
                value.Rounds
                    .Select(round => new CompletedRoundResponse(
                        round.Id,
                        round.Title,
                        round.Votes
                            .Select(vote => new CompletedVoteResponse(vote.ParticipantId, vote.Card))
                            .ToList(),
                        round.FinalEstimate,
                        round.StartedAt,
                        round.EndedAt))
                    .ToList())));
    }

    private static async Task<IResult> StartRound(
        Guid id,
        StartRoundRequest request,
        IMediator mediator,
        HttpContext http,
        CancellationToken ct)
    {
        var participantId = ResolveParticipantId(http, request.CallerParticipantId);
        var result = await mediator.Send(new StartRoundCommand(id, participantId, request.Title), ct);

        return result.ToHttpResult(value =>
            TypedResults.Ok(new StartRoundResponse(value.RoomId, value.RoundId)));
    }

    private static async Task<IResult> SubmitVote(
        Guid id,
        SubmitVoteRequest request,
        IMediator mediator,
        HttpContext http,
        CancellationToken ct)
    {
        var participantId = ResolveParticipantId(http, request.ParticipantId);
        var result = await mediator.Send(new SubmitVoteCommand(id, participantId, request.Card), ct);

        return result.ToHttpResult(TypedResults.NoContent());
    }

    private static async Task<IResult> RevealVotes(
        Guid id,
        ModeratorActionRequest request,
        IMediator mediator,
        HttpContext http,
        CancellationToken ct)
    {
        var participantId = ResolveParticipantId(http, request.CallerParticipantId);
        var result = await mediator.Send(new RevealVotesCommand(id, participantId), ct);

        return result.ToHttpResult(TypedResults.NoContent());
    }

    private static async Task<IResult> ResetRound(
        Guid id,
        ModeratorActionRequest request,
        IMediator mediator,
        HttpContext http,
        CancellationToken ct)
    {
        var participantId = ResolveParticipantId(http, request.CallerParticipantId);
        var result = await mediator.Send(new ResetRoundCommand(id, participantId), ct);

        return result.ToHttpResult(TypedResults.NoContent());
    }

    private static async Task<IResult> EndRound(
        Guid id,
        EndRoundRequest request,
        IMediator mediator,
        HttpContext http,
        CancellationToken ct)
    {
        var participantId = ResolveParticipantId(http, request.CallerParticipantId);
        var result = await mediator.Send(new EndRoundCommand(id, participantId, request.FinalEstimate), ct);

        return result.ToHttpResult(TypedResults.NoContent());
    }

    private static async Task<IResult> PromoteModerator(
        Guid id,
        Guid participantId,
        IMediator mediator,
        HttpContext http,
        CancellationToken ct)
    {
        var callerId = ResolveParticipantId(http, null);
        var result = await mediator.Send(new PromoteModeratorCommand(id, callerId, participantId), ct);

        return result.ToHttpResult(TypedResults.NoContent());
    }

    private static async Task<IResult> DemoteModerator(
        Guid id,
        Guid participantId,
        IMediator mediator,
        HttpContext http,
        CancellationToken ct)
    {
        var callerId = ResolveParticipantId(http, null);
        var result = await mediator.Send(new DemoteModeratorCommand(id, callerId, participantId), ct);

        return result.ToHttpResult(TypedResults.NoContent());
    }

    private static async Task<IResult> ChangeRole(
        Guid id,
        ChangeRoleRequest request,
        IMediator mediator,
        HttpContext http,
        CancellationToken ct)
    {
        var participantId = ResolveParticipantId(http, request.ParticipantId);
        var role = Enum.TryParse<ParticipantRole>(request.Role, ignoreCase: true, out var parsedRole)
            ? parsedRole
            : (ParticipantRole)(-1);

        var result = await mediator.Send(new ChangeRoleCommand(id, participantId, role), ct);

        return result.ToHttpResult(TypedResults.NoContent());
    }

    private static async Task<IResult> LeaveRoom(
        Guid id,
        IMediator mediator,
        HttpContext http,
        CancellationToken ct)
    {
        var participantId = ResolveParticipantId(http, null);
        var result = await mediator.Send(new LeaveRoomCommand(id, participantId), ct);

        return result.ToHttpResult(TypedResults.NoContent());
    }

    private static async Task<IResult> RemoveParticipant(
        Guid id,
        Guid participantId,
        IMediator mediator,
        HttpContext http,
        CancellationToken ct)
    {
        var callerId = ResolveParticipantId(http, null);
        var result = await mediator.Send(new RemoveParticipantCommand(id, callerId, participantId), ct);

        return result.ToHttpResult(TypedResults.NoContent());
    }
}

public sealed record CreateRoomRequest(
    string Name,
    string OwnerDisplayName,
    string? Password = null,
    Guid? OwnerParticipantId = null);

public sealed record CreateRoomResponse(Guid RoomId, Guid OwnerParticipantId);

public sealed record JoinRoomRequest(
    string DisplayName,
    string Role = "Voter",
    string? Password = null,
    Guid? ParticipantId = null);

public sealed record JoinRoomResponse(Guid RoomId, Guid ParticipantId);

public sealed record GetRoomResponse(
    Guid Id,
    string Name,
    Guid OwnerId,
    bool IsPasswordProtected,
    IReadOnlyList<RoomParticipantResponse> Participants,
    IReadOnlyList<Guid> ModeratorIds,
    CurrentRoundResponse? CurrentRound);

public sealed record RoomParticipantResponse(
    Guid Id,
    string DisplayName,
    string Role);

public sealed record CurrentRoundResponse(
    Guid Id,
    string? Title,
    string Phase,
    IReadOnlyList<VoteResponse> Votes);

public sealed record VoteResponse(
    Guid ParticipantId,
    string? Card,
    bool IsRevealed);

public sealed record StartRoundRequest(
    string? Title = null,
    Guid? CallerParticipantId = null);

public sealed record StartRoundResponse(Guid RoomId, Guid RoundId);

public sealed record SubmitVoteRequest(
    string Card,
    Guid? ParticipantId = null);

public sealed record ModeratorActionRequest(Guid? CallerParticipantId = null);

public sealed record EndRoundRequest(
    string? FinalEstimate = null,
    Guid? CallerParticipantId = null);

public sealed record ChangeRoleRequest(
    string Role,
    Guid? ParticipantId = null);

public sealed record GetParticipantRoomsResponse(
    IReadOnlyList<ParticipantRoomSummaryResponse> Rooms);

public sealed record ParticipantRoomSummaryResponse(
    Guid Id,
    string Name,
    int CompletedRoundCount,
    DateTimeOffset LastActiveAt);

public sealed record RoomHistoryResponse(
    Guid RoomId,
    IReadOnlyList<CompletedRoundResponse> Rounds);

public sealed record CompletedRoundResponse(
    Guid Id,
    string? Title,
    IReadOnlyList<CompletedVoteResponse> Votes,
    string? FinalEstimate,
    DateTimeOffset StartedAt,
    DateTimeOffset EndedAt);

public sealed record CompletedVoteResponse(
    Guid ParticipantId,
    string Card);
