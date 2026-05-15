using FluentValidation;
using PokerPlanning.Domain.Participants;
using PokerPlanning.Domain.Rooms;

namespace PokerPlanning.Application.Features.CreateRoom;

public sealed class CreateRoomValidator : AbstractValidator<CreateRoomCommand>
{
    public CreateRoomValidator()
    {
        RuleFor(c => c.Name)
            .NotEmpty()
            .MinimumLength(Room.MinNameLength)
            .MaximumLength(Room.MaxNameLength);

        RuleFor(c => c.OwnerParticipantId)
            .NotEqual(Guid.Empty);

        RuleFor(c => c.OwnerDisplayName)
            .NotEmpty()
            .MaximumLength(Participant.MaxDisplayNameLength);

        When(c => c.Password is not null, () =>
        {
            RuleFor(c => c.Password!)
                .MinimumLength(4)
                .MaximumLength(128);
        });
    }
}
