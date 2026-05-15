using FluentValidation;
using PokerPlanning.Domain.Participants;

namespace PokerPlanning.Application.Features.JoinRoom;

public sealed class JoinRoomValidator : AbstractValidator<JoinRoomCommand>
{
    public JoinRoomValidator()
    {
        RuleFor(c => c.RoomId)
            .NotEqual(Guid.Empty);

        RuleFor(c => c.ParticipantId)
            .NotEqual(Guid.Empty);

        RuleFor(c => c.DisplayName)
            .NotEmpty()
            .MaximumLength(Participant.MaxDisplayNameLength);

        RuleFor(c => c.Role)
            .IsInEnum();

        When(c => c.Password is not null, () =>
        {
            RuleFor(c => c.Password!)
                .MinimumLength(4)
                .MaximumLength(128);
        });
    }
}
