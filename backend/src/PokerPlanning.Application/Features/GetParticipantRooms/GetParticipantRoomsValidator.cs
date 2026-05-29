using FluentValidation;

namespace PokerPlanning.Application.Features.GetParticipantRooms;

public sealed class GetParticipantRoomsValidator : AbstractValidator<GetParticipantRoomsQuery>
{
    public GetParticipantRoomsValidator()
    {
        RuleFor(q => q.CallerUserId).NotEqual(Guid.Empty);
    }
}
