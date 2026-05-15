using FluentValidation;

namespace PokerPlanning.Application.Features.ChangeRole;

public sealed class ChangeRoleValidator : AbstractValidator<ChangeRoleCommand>
{
    public ChangeRoleValidator()
    {
        RuleFor(c => c.RoomId).NotEqual(Guid.Empty);
        RuleFor(c => c.ParticipantId).NotEqual(Guid.Empty);
        RuleFor(c => c.Role).IsInEnum();
    }
}
