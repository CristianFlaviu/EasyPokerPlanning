using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using PokerPlanning.Domain.Common;

namespace PokerPlanning.Api.Common;

public static class ResultExtensions
{
    public static IResult ToHttpResult<T>(this Result<T> result, Func<T, IResult> onSuccess) =>
        result.IsSuccess
            ? onSuccess(result.Value)
            : Problem(result.Error);

    public static IResult ToHttpResult(this Result result, IResult onSuccess) =>
        result.IsSuccess ? onSuccess : Problem(result.Error);

    public static ProblemHttpResult Problem(Error error)
    {
        var (statusCode, title) = error.Code switch
        {
            "Validation" => (StatusCodes.Status400BadRequest, "Validation failed"),
            "Room.NotFound" => (StatusCodes.Status404NotFound, "Not found"),
            "Room.InvalidPassword" => (StatusCodes.Status403Forbidden, "Forbidden"),
            "Room.NotAuthorized" => (StatusCodes.Status403Forbidden, "Forbidden"),
            "Room.OwnerCannotLeave" => (StatusCodes.Status403Forbidden, "Forbidden"),
            "Room.OwnerCannotBeRemoved" => (StatusCodes.Status403Forbidden, "Forbidden"),
            "Room.ParticipantNotFound" => (StatusCodes.Status404NotFound, "Not found"),
            _ => (StatusCodes.Status400BadRequest, "Bad request"),
        };

        return TypedResults.Problem(
            detail: error.Message,
            statusCode: statusCode,
            title: title,
            type: error.Code);
    }
}
