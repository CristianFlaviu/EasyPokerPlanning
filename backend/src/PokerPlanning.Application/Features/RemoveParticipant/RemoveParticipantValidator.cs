using FluentValidation;

namespace PokerPlanning.Application.Features.RemoveParticipant;

public sealed class RemoveParticipantValidator : AbstractValidator<RemoveParticipantCommand>
{
    public RemoveParticipantValidator()
    {
        RuleFor(x => x.RoomId)
            .NotEmpty().WithMessage("Room ID is required.");

        RuleFor(x => x.CallerId)
            .NotEmpty().WithMessage("Caller ID is required.");

        RuleFor(x => x.ParticipantId)
            .NotEmpty().WithMessage("Participant ID is required.");
    }
}
