using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using PokerPlanning.Api.Common;
using PokerPlanning.Application.Abstractions.Security;
using PokerPlanning.Application.Features.SubmitFeedback;

namespace PokerPlanning.Api.Endpoints;

public static class FeedbackEndpoints
{
    public static IEndpointRouteBuilder MapFeedbackEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/feedback").WithTags("Feedback");

        group.MapPost("", SubmitFeedback)
            .WithName("SubmitFeedback")
            .WithSummary("Submit user feedback. Available to anyone, signed in or not.");

        return app;
    }

    private static async Task<IResult> SubmitFeedback(
        SubmitFeedbackRequest request,
        IMediator mediator,
        IUserContext userContext,
        CancellationToken ct)
    {
        var command = new SubmitFeedbackCommand(
            request.Name,
            request.Email,
            request.Message,
            userContext.CurrentUserId);

        var result = await mediator.Send(command, ct);

        return result.ToHttpResult(TypedResults.NoContent());
    }
}

public sealed record SubmitFeedbackRequest(
    string Message,
    string? Name = null,
    string? Email = null);
