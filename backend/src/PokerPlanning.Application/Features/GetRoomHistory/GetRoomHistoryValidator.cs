using FluentValidation;

namespace PokerPlanning.Application.Features.GetRoomHistory;

public sealed class GetRoomHistoryValidator : AbstractValidator<GetRoomHistoryQuery>
{
    public GetRoomHistoryValidator()
    {
        RuleFor(q => q.RoomId).NotEqual(Guid.Empty);
    }
}
