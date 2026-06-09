using FluentValidation;
using FeedbackEntity = PokerPlanning.Domain.Feedback.Feedback;

namespace PokerPlanning.Application.Features.SubmitFeedback;

public sealed class SubmitFeedbackValidator : AbstractValidator<SubmitFeedbackCommand>
{
    public SubmitFeedbackValidator()
    {
        RuleFor(c => c.Message).NotEmpty().MaximumLength(FeedbackEntity.MaxMessageLength);
        RuleFor(c => c.Name).MaximumLength(FeedbackEntity.MaxNameLength);
        RuleFor(c => c.Email).MaximumLength(FeedbackEntity.MaxEmailLength);
    }
}
