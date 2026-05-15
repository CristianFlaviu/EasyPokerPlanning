using FluentValidation;

namespace PokerPlanning.Application.Features.EndRound;

public sealed class EndRoundValidator : AbstractValidator<EndRoundCommand>
{
    public EndRoundValidator()
    {
        RuleFor(c => c.RoomId).NotEqual(Guid.Empty);
        RuleFor(c => c.CallerParticipantId).NotEqual(Guid.Empty);
    }
}
