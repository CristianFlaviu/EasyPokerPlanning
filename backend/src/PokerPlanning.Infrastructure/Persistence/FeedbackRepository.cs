using PokerPlanning.Application.Abstractions.Persistence;
using FeedbackEntity = PokerPlanning.Domain.Feedback.Feedback;

namespace PokerPlanning.Infrastructure.Persistence;

public sealed class FeedbackRepository(PokerPlanningDbContext db) : IFeedbackRepository
{
    public async Task AddAsync(FeedbackEntity feedback, CancellationToken ct) =>
        await db.Feedbacks.AddAsync(feedback, ct);

    public Task SaveChangesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}
