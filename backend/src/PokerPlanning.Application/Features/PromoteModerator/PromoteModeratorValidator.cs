using FluentValidation;

namespace PokerPlanning.Application.Features.PromoteModerator;

public sealed class PromoteModeratorValidator : AbstractValidator<PromoteModeratorCommand>
{
    public PromoteModeratorValidator()
    {
        RuleFor(c => c.RoomId).NotEqual(Guid.Empty);
        RuleFor(c => c.CallerParticipantId).NotEqual(Guid.Empty);
        RuleFor(c => c.ParticipantId).NotEqual(Guid.Empty);
    }
}
