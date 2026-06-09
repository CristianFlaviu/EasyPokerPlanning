using FluentValidation;

namespace PokerPlanning.Application.Features.ThrowReaction;

public sealed class ThrowReactionValidator : AbstractValidator<ThrowReactionCommand>
{
    public ThrowReactionValidator()
    {
        RuleFor(c => c.RoomId).NotEqual(Guid.Empty);
        RuleFor(c => c.FromParticipantId).NotEqual(Guid.Empty);
        RuleFor(c => c.ToParticipantId).NotEqual(Guid.Empty);
        RuleFor(c => c.Emoji).Must(ReactionEmojis.Allowed.Contains)
            .WithMessage("Unsupported reaction.");
    }
}
