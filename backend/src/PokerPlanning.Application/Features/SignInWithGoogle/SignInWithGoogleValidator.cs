using FluentValidation;

namespace PokerPlanning.Application.Features.SignInWithGoogle;

public sealed class SignInWithGoogleValidator : AbstractValidator<SignInWithGoogleCommand>
{
    public SignInWithGoogleValidator()
    {
        RuleFor(c => c.GoogleSubject).NotEmpty();
        RuleFor(c => c.Email).NotEmpty();
        RuleFor(c => c.Name).NotEmpty();
    }
}
