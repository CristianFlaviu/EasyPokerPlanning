using MediatR;
using PokerPlanning.Application.Abstractions.Persistence;
using PokerPlanning.Application.Abstractions.Time;
using PokerPlanning.Domain.Common;
using FeedbackEntity = PokerPlanning.Domain.Feedback.Feedback;

namespace PokerPlanning.Application.Features.SubmitFeedback;

public sealed class SubmitFeedbackHandler(
    IFeedbackRepository feedback,
    IClock clock)
    : IRequestHandler<SubmitFeedbackCommand, Result>
{
    public async Task<Result> Handle(SubmitFeedbackCommand cmd, CancellationToken ct)
    {
        var result = FeedbackEntity.Create(cmd.Name, cmd.Email, cmd.Message, cmd.UserId, clock.UtcNow);
        if (result.IsFailure)
            return Result.Failure(result.Error);

        await feedback.AddAsync(result.Value, ct);
        await feedback.SaveChangesAsync(ct);
        return Result.Success();
    }
}
