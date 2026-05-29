using FluentValidation;

namespace PokerPlanning.Application.Features.UploadAvatar;

public sealed class UploadAvatarValidator : AbstractValidator<UploadAvatarCommand>
{
    public const long MaxBytes = 5 * 1024 * 1024;

    public static readonly string[] AllowedContentTypes =
        ["image/jpeg", "image/png", "image/webp"];

    public UploadAvatarValidator()
    {
        RuleFor(c => c.UserId)
            .NotEqual(Guid.Empty);

        RuleFor(c => c.ContentType)
            .Must(ct => AllowedContentTypes.Contains(ct))
            .WithMessage("Avatar must be a JPEG, PNG, or WebP image.");

        RuleFor(c => c.Length)
            .GreaterThan(0)
            .LessThanOrEqualTo(MaxBytes)
            .WithMessage($"Avatar must be at most {MaxBytes / (1024 * 1024)} MB.");
    }
}
