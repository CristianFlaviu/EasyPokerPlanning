using FluentValidation;

namespace PokerPlanning.Application.Features.GetRoom;

public sealed class GetRoomValidator : AbstractValidator<GetRoomQuery>
{
    public GetRoomValidator()
    {
        RuleFor(q => q.RoomId)
            .NotEqual(Guid.Empty);
    }
}
