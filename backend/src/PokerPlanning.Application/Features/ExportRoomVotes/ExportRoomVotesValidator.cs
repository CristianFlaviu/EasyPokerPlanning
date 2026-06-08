using FluentValidation;

namespace PokerPlanning.Application.Features.ExportRoomVotes;

public sealed class ExportRoomVotesValidator : AbstractValidator<ExportRoomVotesQuery>
{
    public ExportRoomVotesValidator()
    {
        RuleFor(q => q.RoomId).NotEqual(Guid.Empty);
    }
}
