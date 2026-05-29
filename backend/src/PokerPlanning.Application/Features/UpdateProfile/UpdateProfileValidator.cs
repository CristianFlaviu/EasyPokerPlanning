using FluentValidation;
using PokerPlanning.Domain.Users;

namespace PokerPlanning.Application.Features.UpdateProfile;

public sealed class UpdateProfileValidator : AbstractValidator<UpdateProfileCommand>
{
    public UpdateProfileValidator()
    {
        RuleFor(c => c.UserId)
            .NotEqual(Guid.Empty);

        RuleFor(c => c.DisplayName)
            .NotEmpty()
            .MinimumLength(User.MinDisplayNameLength)
            .MaximumLength(User.MaxDisplayNameLength);

        When(c => c.AvatarUrl is not null, () =>
        {
            RuleFor(c => c.AvatarUrl!)
                .MaximumLength(User.MaxAvatarUrlLength);
        });
    }
}
