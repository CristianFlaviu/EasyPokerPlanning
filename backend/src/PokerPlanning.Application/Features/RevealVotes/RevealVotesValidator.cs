using FluentValidation;

namespace PokerPlanning.Application.Features.RevealVotes;

public sealed class RevealVotesValidator : AbstractValidator<RevealVotesCommand>
{
    public RevealVotesValidator()
    {
        RuleFor(c => c.RoomId).NotEqual(Guid.Empty);
        RuleFor(c => c.CallerParticipantId).NotEqual(Guid.Empty);
    }
}
