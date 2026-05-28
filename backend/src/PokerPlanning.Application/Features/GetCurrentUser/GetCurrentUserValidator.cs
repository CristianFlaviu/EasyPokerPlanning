using FluentValidation;

namespace PokerPlanning.Application.Features.GetCurrentUser;

public sealed class GetCurrentUserValidator : AbstractValidator<GetCurrentUserQuery>
{
    public GetCurrentUserValidator()
    {
        RuleFor(q => q.UserId).NotEqual(Guid.Empty);
    }
}
