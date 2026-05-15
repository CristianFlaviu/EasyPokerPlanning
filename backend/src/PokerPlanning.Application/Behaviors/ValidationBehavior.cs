using FluentValidation;
using MediatR;
using PokerPlanning.Domain.Common;

namespace PokerPlanning.Application.Behaviors;

public sealed class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
    where TResponse : Result
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!validators.Any())
            return await next(cancellationToken);

        var context = new ValidationContext<TRequest>(request);
        var failures = (await Task.WhenAll(validators.Select(v => v.ValidateAsync(context, cancellationToken))))
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .ToList();

        if (failures.Count == 0)
            return await next(cancellationToken);

        var error = new Error(
            "Validation",
            string.Join("; ", failures.Select(f => $"{f.PropertyName}: {f.ErrorMessage}")));

        return CreateValidationFailure<TResponse>(error);
    }

    private static TResult CreateValidationFailure<TResult>(Error error) where TResult : Result
    {
        if (typeof(TResult) == typeof(Result))
            return (TResult)Result.Failure(error);

        var resultType = typeof(TResult).GetGenericArguments()[0];
        var method = typeof(Result)
            .GetMethods()
            .First(m => m.Name == nameof(Result.Failure) && m.IsGenericMethod)
            .MakeGenericMethod(resultType);

        return (TResult)method.Invoke(null, [error])!;
    }
}
