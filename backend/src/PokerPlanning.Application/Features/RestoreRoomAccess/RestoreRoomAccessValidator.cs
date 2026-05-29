using FluentValidation;

namespace PokerPlanning.Application.Features.RestoreRoomAccess;

public sealed class RestoreRoomAccessValidator : AbstractValidator<RestoreRoomAccessCommand>
{
    public RestoreRoomAccessValidator()
    {
        RuleFor(c => c.RoomId).NotEqual(Guid.Empty);
        RuleFor(c => c.CallerUserId).NotEqual(Guid.Empty);
    }
}
