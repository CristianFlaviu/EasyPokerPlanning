using FluentValidation;
using PokerPlanning.Domain.Rooms;

namespace PokerPlanning.Application.Features.StartRound;

public sealed class StartRoundValidator : AbstractValidator<StartRoundCommand>
{
    public StartRoundValidator()
    {
        RuleFor(c => c.RoomId).NotEqual(Guid.Empty);
        RuleFor(c => c.CallerParticipantId).NotEqual(Guid.Empty);

        When(c => c.Title is not null, () =>
        {
            RuleFor(c => c.Title!)
                .MaximumLength(Round.MaxTitleLength);
        });
    }
}
