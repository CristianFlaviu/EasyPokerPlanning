using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using PokerPlanning.Api.Common;
using PokerPlanning.Application.Features.CreateRoom;
using PokerPlanning.Application.Features.GetRoom;
using PokerPlanning.Application.Features.JoinRoom;
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

        group.MapPost("{id:guid}/join", JoinRoom)
            .WithName("JoinRoom")
            .WithSummary("Join an existing poker planning room.");

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
        CancellationToken ct)
    {
        var result = await mediator.Send(new GetRoomQuery(id), ct);

        return result.ToHttpResult(value =>
            TypedResults.Ok(new GetRoomResponse(
                value.Id,
                value.Name,
                value.OwnerId,
                value.IsPasswordProtected,
                value.Participants
                    .Select(p => new RoomParticipantResponse(p.Id, p.DisplayName, p.Role))
                    .ToList())));
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
    IReadOnlyList<RoomParticipantResponse> Participants);

public sealed record RoomParticipantResponse(
    Guid Id,
    string DisplayName,
    string Role);
