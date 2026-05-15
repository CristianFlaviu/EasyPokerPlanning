using FluentValidation;

namespace PokerPlanning.Application.Features.ResetRound;

public sealed class ResetRoundValidator : AbstractValidator<ResetRoundCommand>
{
    public ResetRoundValidator()
    {
        RuleFor(c => c.RoomId).NotEqual(Guid.Empty);
        RuleFor(c => c.CallerParticipantId).NotEqual(Guid.Empty);
    }
}
