using System.Globalization;
using System.Security.Claims;
using System.Text;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using PokerPlanning.Api.Common;
using PokerPlanning.Application.Abstractions.Security;
using PokerPlanning.Application.Features.ChangeRole;
using PokerPlanning.Application.Features.DemoteModerator;
using PokerPlanning.Application.Features.CreateRoom;
using PokerPlanning.Application.Features.EndRound;
using PokerPlanning.Application.Features.ExportRoomVotes;
using PokerPlanning.Application.Features.GetRoom;
using PokerPlanning.Application.Features.GetParticipantRooms;
using PokerPlanning.Application.Features.GetRoomHistory;
using PokerPlanning.Application.Features.JoinRoom;
using PokerPlanning.Application.Features.LeaveRoom;
using PokerPlanning.Application.Features.PromoteModerator;
using PokerPlanning.Application.Features.RemoveParticipant;
using PokerPlanning.Application.Features.ResetRound;
using PokerPlanning.Application.Features.RestoreRoomAccess;
using PokerPlanning.Application.Features.RevealVotes;
using PokerPlanning.Application.Features.StartRound;
using PokerPlanning.Application.Features.SubmitVote;
using PokerPlanning.Application.Features.ThrowReaction;
using PokerPlanning.Domain.Participants;
using PokerPlanning.Domain.Rooms;

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

        group.MapGet("{id:guid}/votes.csv", ExportRoomVotes)
            .WithName("ExportRoomVotes")
            .WithSummary("Export completed-round votes as CSV.");

        group.MapPost("{id:guid}/join", JoinRoom)
            .WithName("JoinRoom")
            .WithSummary("Join an existing poker planning room.");

        group.MapPost("{id:guid}/access", RestoreRoomAccess)
            .WithName("RestoreRoomAccess")
            .WithSummary("Restore room access for a signed-in account already linked to the room.");

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

        group.MapPost("{id:guid}/reactions", ThrowReaction)
            .WithName("ThrowReaction")
            .WithSummary("Throw an emoji reaction at another participant.");

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
        IUserContext userContext,
        CancellationToken ct)
    {
        // At creation the caller establishes (claims) their stable cross-room participant id.
        // It is bound into the returned seat token; from then on the token is the credential.
        var participantId = ResolveClaimedParticipantId(http, request.OwnerParticipantId);

        var command = new CreateRoomCommand(
            request.Name,
            request.Password,
            participantId,
            request.OwnerDisplayName,
            userContext.CurrentUserId);

        var result = await mediator.Send(command, ct);

        return result.ToHttpResult(value =>
            TypedResults.Created(
                $"/rooms/{value.RoomId}",
                new CreateRoomResponse(value.RoomId, value.OwnerParticipantId, value.AccessToken)));
    }

    private static async Task<IResult> GetRoom(
        Guid id,
        IMediator mediator,
        HttpContext http,
        IRoomAccessTokenService tokens,
        CancellationToken ct)
    {
        var hasAccess = TryResolveSeat(http, id, tokens, out var participantId);
        var result = await mediator.Send(new GetRoomQuery(id, participantId, hasAccess), ct);

        return result.ToHttpResult(value =>
            TypedResults.Ok(new GetRoomResponse(
                value.Id,
                value.Name,
                value.OwnerId,
                value.IsPasswordProtected,
                value.Participants
                    .Select(p => new RoomParticipantResponse(p.Id, p.DisplayName, p.Role, p.AvatarUrl))
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
        IRoomAccessTokenService tokens,
        IUserContext userContext,
        CancellationToken ct)
    {
        var participantId = ResolveClaimedParticipantId(http, request.ParticipantId);
        var existingSeatConfirmed =
            TryResolveSeat(http, id, tokens, out var tokenParticipantId)
            && tokenParticipantId == participantId;
        var role = Enum.TryParse<ParticipantRole>(request.Role, ignoreCase: true, out var parsedRole)
            ? parsedRole
            : (ParticipantRole)(-1);

        var command = new JoinRoomCommand(
            id,
            participantId,
            request.DisplayName,
            role,
            request.Password,
            existingSeatConfirmed,
            userContext.CurrentUserId);

        var result = await mediator.Send(command, ct);

        return result.ToHttpResult(value =>
            TypedResults.Ok(new JoinRoomResponse(value.RoomId, value.ParticipantId, value.AccessToken)));
    }

    private static async Task<IResult> RestoreRoomAccess(
        Guid id,
        IMediator mediator,
        IUserContext userContext,
        CancellationToken ct)
    {
        if (userContext.CurrentUserId is null)
            return TypedResults.Unauthorized();

        var result = await mediator.Send(
            new RestoreRoomAccessCommand(id, userContext.CurrentUserId.Value),
            ct);

        return result.ToHttpResult(value =>
            TypedResults.Ok(new RestoreRoomAccessResponse(value.RoomId, value.ParticipantId, value.AccessToken)));
    }

    private static async Task<IResult> GetParticipantRooms(
        IMediator mediator,
        IUserContext userContext,
        CancellationToken ct)
    {
        if (userContext.CurrentUserId is null)
            return TypedResults.Unauthorized();

        var result = await mediator.Send(new GetParticipantRoomsQuery(userContext.CurrentUserId.Value), ct);

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
        HttpContext http,
        IUserContext userContext,
        CancellationToken ct)
    {
        if (userContext.CurrentUserId is null)
            return TypedResults.Unauthorized();

        var result = await mediator.Send(
            new GetRoomHistoryQuery(id, userContext.CurrentUserId),
            ct);

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

    private static async Task<IResult> ExportRoomVotes(
        Guid id,
        IMediator mediator,
        IUserContext userContext,
        CancellationToken ct)
    {
        if (userContext.CurrentUserId is null)
            return TypedResults.Unauthorized();

        var result = await mediator.Send(
            new ExportRoomVotesQuery(id, userContext.CurrentUserId),
            ct);

        // Success returns a file rather than JSON, so the standard ToHttpResult mapping doesn't fit.
        if (result.IsFailure)
            return ResultExtensions.Problem(result.Error);

        var csv = BuildVotesCsv(result.Value);

        // Lead with a UTF-8 BOM so Excel renders accented names and the "?" card correctly.
        var bytes = new byte[Encoding.UTF8.GetPreamble().Length + Encoding.UTF8.GetByteCount(csv)];
        Encoding.UTF8.GetPreamble().CopyTo(bytes, 0);
        Encoding.UTF8.GetBytes(csv, 0, csv.Length, bytes, Encoding.UTF8.GetPreamble().Length);

        return Results.File(bytes, "text/csv; charset=utf-8", $"{SlugifyFileName(result.Value.RoomName)}-votes.csv");
    }

    private static string BuildVotesCsv(ExportRoomVotesResult export)
    {
        // Wide layout: one row per round, one column per voter (cell = that voter's card, blank if none).
        List<string> header = ["Round", "Title"];
        header.AddRange(export.Voters.Select(v => v.Name));
        header.Add("FinalEstimate");

        var rows = export.Rounds.Select(round =>
        {
            var row = new List<string>(header.Count)
            {
                round.Number.ToString(CultureInfo.InvariantCulture),
                round.Title ?? string.Empty,
            };
            foreach (var voter in export.Voters)
                row.Add(round.VotesByParticipant.TryGetValue(voter.ParticipantId, out var card) ? card : string.Empty);
            row.Add(round.FinalEstimate ?? string.Empty);
            return (IReadOnlyList<string>)row;
        });

        return CsvWriter.Build(header, rows);
    }

    // Produces a filename-safe slug; falls back to "room" when nothing usable remains.
    private static string SlugifyFileName(string name)
    {
        var slug = new string(name
            .Trim()
            .ToLowerInvariant()
            .Select(c => char.IsLetterOrDigit(c) ? c : '-')
            .ToArray())
            .Trim('-');

        while (slug.Contains("--"))
            slug = slug.Replace("--", "-");

        return string.IsNullOrEmpty(slug) ? "room" : slug;
    }

    private static async Task<IResult> StartRound(
        Guid id,
        StartRoundRequest request,
        IMediator mediator,
        HttpContext http,
        IRoomAccessTokenService tokens,
        CancellationToken ct)
    {
        if (!TryResolveSeat(http, id, tokens, out var participantId))
            return RoomAccessRequired();

        var result = await mediator.Send(new StartRoundCommand(id, participantId, request.Title), ct);

        return result.ToHttpResult(value =>
            TypedResults.Ok(new StartRoundResponse(value.RoomId, value.RoundId)));
    }

    private static async Task<IResult> SubmitVote(
        Guid id,
        SubmitVoteRequest request,
        IMediator mediator,
        HttpContext http,
        IRoomAccessTokenService tokens,
        CancellationToken ct)
    {
        if (!TryResolveSeat(http, id, tokens, out var participantId))
            return RoomAccessRequired();

        var result = await mediator.Send(new SubmitVoteCommand(id, participantId, request.Card), ct);

        return result.ToHttpResult(TypedResults.NoContent());
    }

    private static async Task<IResult> RevealVotes(
        Guid id,
        IMediator mediator,
        HttpContext http,
        IRoomAccessTokenService tokens,
        CancellationToken ct)
    {
        if (!TryResolveSeat(http, id, tokens, out var participantId))
            return RoomAccessRequired();

        var result = await mediator.Send(new RevealVotesCommand(id, participantId), ct);

        return result.ToHttpResult(TypedResults.NoContent());
    }

    private static async Task<IResult> ResetRound(
        Guid id,
        IMediator mediator,
        HttpContext http,
        IRoomAccessTokenService tokens,
        CancellationToken ct)
    {
        if (!TryResolveSeat(http, id, tokens, out var participantId))
            return RoomAccessRequired();

        var result = await mediator.Send(new ResetRoundCommand(id, participantId), ct);

        return result.ToHttpResult(TypedResults.NoContent());
    }

    private static async Task<IResult> EndRound(
        Guid id,
        EndRoundRequest request,
        IMediator mediator,
        HttpContext http,
        IRoomAccessTokenService tokens,
        CancellationToken ct)
    {
        if (!TryResolveSeat(http, id, tokens, out var participantId))
            return RoomAccessRequired();

        var result = await mediator.Send(new EndRoundCommand(id, participantId, request.FinalEstimate), ct);

        return result.ToHttpResult(TypedResults.NoContent());
    }

    private static async Task<IResult> PromoteModerator(
        Guid id,
        Guid participantId,
        IMediator mediator,
        HttpContext http,
        IRoomAccessTokenService tokens,
        CancellationToken ct)
    {
        if (!TryResolveSeat(http, id, tokens, out var callerId))
            return RoomAccessRequired();

        var result = await mediator.Send(new PromoteModeratorCommand(id, callerId, participantId), ct);

        return result.ToHttpResult(TypedResults.NoContent());
    }

    private static async Task<IResult> DemoteModerator(
        Guid id,
        Guid participantId,
        IMediator mediator,
        HttpContext http,
        IRoomAccessTokenService tokens,
        CancellationToken ct)
    {
        if (!TryResolveSeat(http, id, tokens, out var callerId))
            return RoomAccessRequired();

        var result = await mediator.Send(new DemoteModeratorCommand(id, callerId, participantId), ct);

        return result.ToHttpResult(TypedResults.NoContent());
    }

    private static async Task<IResult> ThrowReaction(
        Guid id,
        ThrowReactionRequest request,
        IMediator mediator,
        HttpContext http,
        IRoomAccessTokenService tokens,
        CancellationToken ct)
    {
        // The thrower is the token-bound seat, never the request body, so reactions
        // cannot be spoofed as coming from another participant.
        if (!TryResolveSeat(http, id, tokens, out var fromParticipantId))
            return RoomAccessRequired();

        var result = await mediator.Send(
            new ThrowReactionCommand(id, fromParticipantId, request.ToParticipantId, request.Emoji),
            ct);

        return result.ToHttpResult(TypedResults.NoContent());
    }

    private static async Task<IResult> ChangeRole(
        Guid id,
        ChangeRoleRequest request,
        IMediator mediator,
        HttpContext http,
        IRoomAccessTokenService tokens,
        CancellationToken ct)
    {
        // "me" endpoint: the caller can only change their own role, derived from the token.
        if (!TryResolveSeat(http, id, tokens, out var participantId))
            return RoomAccessRequired();

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
        IRoomAccessTokenService tokens,
        CancellationToken ct)
    {
        if (!TryResolveSeat(http, id, tokens, out var participantId))
            return RoomAccessRequired();

        var result = await mediator.Send(new LeaveRoomCommand(id, participantId), ct);

        return result.ToHttpResult(TypedResults.NoContent());
    }

    private static async Task<IResult> RemoveParticipant(
        Guid id,
        Guid participantId,
        IMediator mediator,
        HttpContext http,
        IRoomAccessTokenService tokens,
        CancellationToken ct)
    {
        if (!TryResolveSeat(http, id, tokens, out var callerId))
            return RoomAccessRequired();

        var result = await mediator.Send(new RemoveParticipantCommand(id, callerId, participantId), ct);

        return result.ToHttpResult(TypedResults.NoContent());
    }

    // Resolves the caller's seat from the signed room access token (X-Room-Token header).
    // Returns false when no valid token is present for this room.
    private static bool TryResolveSeat(
        HttpContext http,
        Guid roomId,
        IRoomAccessTokenService tokens,
        out Guid participantId)
    {
        participantId = Guid.Empty;

        var token = http.Request.Headers["X-Room-Token"].ToString();
        if (string.IsNullOrWhiteSpace(token))
            return false;

        if (!tokens.TryValidate(token, new RoomId(roomId), out var access))
            return false;

        if (access.UserId is not null
            && (!TryGetCurrentUserId(http, out var currentUserId) || currentUserId != access.UserId.Value.Value))
        {
            return false;
        }

        participantId = access.ParticipantId.Value;
        return true;
    }

    private static bool TryGetCurrentUserId(HttpContext http, out Guid userId)
    {
        userId = Guid.Empty;
        var sub = http.User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(sub, out userId);
    }

    // Identity claim for create/join/history only (not an authorization credential).
    private static Guid ResolveClaimedParticipantId(HttpContext http, Guid? fromBody)
    {
        if (fromBody is { } id && id != Guid.Empty)
            return id;

        if (http.Request.Headers.TryGetValue("X-Participant-Id", out var header)
            && Guid.TryParse(header.ToString(), out var headerId))
            return headerId;

        return Guid.Empty;
    }

    private static IResult RoomAccessRequired() =>
        TypedResults.Problem(
            detail: "A valid room access token is required. Join the room first.",
            statusCode: StatusCodes.Status401Unauthorized);
}

public sealed record CreateRoomRequest(
    string Name,
    string OwnerDisplayName,
    string? Password = null,
    Guid? OwnerParticipantId = null);

public sealed record CreateRoomResponse(Guid RoomId, Guid OwnerParticipantId, string AccessToken);

public sealed record JoinRoomRequest(
    string DisplayName,
    string Role = "Voter",
    string? Password = null,
    Guid? ParticipantId = null);

public sealed record JoinRoomResponse(Guid RoomId, Guid ParticipantId, string AccessToken);

public sealed record RestoreRoomAccessResponse(Guid RoomId, Guid ParticipantId, string AccessToken);

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
    string Role,
    string? AvatarUrl);

public sealed record CurrentRoundResponse(
    Guid Id,
    string? Title,
    string Phase,
    IReadOnlyList<VoteResponse> Votes);

public sealed record VoteResponse(
    Guid ParticipantId,
    string? Card,
    bool IsRevealed);

public sealed record StartRoundRequest(string? Title = null);

public sealed record StartRoundResponse(Guid RoomId, Guid RoundId);

public sealed record SubmitVoteRequest(string Card);

public sealed record EndRoundRequest(string? FinalEstimate = null);

public sealed record ChangeRoleRequest(string Role);

public sealed record ThrowReactionRequest(Guid ToParticipantId, string Emoji);

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
