using FluentValidation;

namespace PokerPlanning.Application.Features.ConsumeEmailLoginToken;

public sealed class ConsumeEmailLoginTokenValidator : AbstractValidator<ConsumeEmailLoginTokenCommand>
{
    public ConsumeEmailLoginTokenValidator()
    {
        RuleFor(c => c.Token).NotEmpty().Length(64);
    }
}
