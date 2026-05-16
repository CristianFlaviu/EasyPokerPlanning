using FluentValidation;

namespace PokerPlanning.Application.Features.LeaveRoom;

public sealed class LeaveRoomValidator : AbstractValidator<LeaveRoomCommand>
{
    public LeaveRoomValidator()
    {
        RuleFor(c => c.RoomId).NotEqual(Guid.Empty);
        RuleFor(c => c.ParticipantId).NotEqual(Guid.Empty);
    }
}
