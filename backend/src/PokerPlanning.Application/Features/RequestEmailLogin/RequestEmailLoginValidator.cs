using FluentValidation;

namespace PokerPlanning.Application.Features.RequestEmailLogin;

public sealed class RequestEmailLoginValidator : AbstractValidator<RequestEmailLoginCommand>
{
    public RequestEmailLoginValidator()
    {
        RuleFor(c => c.Mode)
            .Must(mode => EmailLoginModes.TryParse(mode, out _))
            .WithMessage("Mode must be either login or signup.");
        RuleFor(c => c.Email).NotEmpty().EmailAddress().MaximumLength(254);
        RuleFor(c => c.ReturnUrl).NotEmpty().MaximumLength(2048);
        RuleFor(c => c.CallbackBaseUrl).NotEmpty().MaximumLength(2048);
        RuleFor(c => c.DisplayName).MaximumLength(80);
    }
}
