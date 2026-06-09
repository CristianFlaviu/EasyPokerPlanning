using FeedbackEntity = PokerPlanning.Domain.Feedback.Feedback;

namespace PokerPlanning.Application.Abstractions.Persistence;

public interface IFeedbackRepository
{
    Task AddAsync(FeedbackEntity feedback, CancellationToken ct);

    Task SaveChangesAsync(CancellationToken ct);
}
