using FluentValidation;

namespace PokerPlanning.Application.Features.DemoteModerator;

public sealed class DemoteModeratorValidator : AbstractValidator<DemoteModeratorCommand>
{
    public DemoteModeratorValidator()
    {
        RuleFor(c => c.RoomId).NotEqual(Guid.Empty);
        RuleFor(c => c.CallerParticipantId).NotEqual(Guid.Empty);
        RuleFor(c => c.ParticipantId).NotEqual(Guid.Empty);
    }
}
