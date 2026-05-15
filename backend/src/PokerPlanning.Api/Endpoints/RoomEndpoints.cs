using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using PokerPlanning.Api.Common;
using PokerPlanning.Application.Features.CreateRoom;

namespace PokerPlanning.Api.Endpoints;

public static class RoomEndpoints
{
    public static IEndpointRouteBuilder MapRoomEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/rooms").WithTags("Rooms");

        group.MapPost("", CreateRoom)
            .WithName("CreateRoom")
            .WithSummary("Create a new poker planning room.");

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
}

public sealed record CreateRoomRequest(
    string Name,
    string OwnerDisplayName,
    string? Password = null,
    Guid? OwnerParticipantId = null);

public sealed record CreateRoomResponse(Guid RoomId, Guid OwnerParticipantId);
