using FluentValidation;

namespace PokerPlanning.Application.Features.SubmitVote;

public sealed class SubmitVoteValidator : AbstractValidator<SubmitVoteCommand>
{
    public SubmitVoteValidator()
    {
        RuleFor(c => c.RoomId).NotEqual(Guid.Empty);
        RuleFor(c => c.ParticipantId).NotEqual(Guid.Empty);
        RuleFor(c => c.Card).NotEmpty();
    }
}
