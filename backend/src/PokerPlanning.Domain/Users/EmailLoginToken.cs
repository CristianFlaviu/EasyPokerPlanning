using PokerPlanning.Domain.Common;

namespace PokerPlanning.Domain.Users;

public sealed class EmailLoginToken
{
    public const int TokenHashLength = 64;
    public const int MaxReturnUrlLength = 2048;

    private EmailLoginToken(
        Guid id,
        string tokenHash,
        string email,
        string? displayName,
        EmailLoginMode mode,
        string returnUrl,
        DateTimeOffset createdAt,
        DateTimeOffset expiresAt)
    {
        Id = id;
        TokenHash = tokenHash;
        Email = email;
        DisplayName = displayName;
        Mode = mode;
        ReturnUrl = returnUrl;
        CreatedAt = createdAt;
        ExpiresAt = expiresAt;
    }

    public Guid Id { get; }
    public string TokenHash { get; }
    public string Email { get; }
    public string? DisplayName { get; }
    public EmailLoginMode Mode { get; }
    public string ReturnUrl { get; }
    public DateTimeOffset CreatedAt { get; }
    public DateTimeOffset ExpiresAt { get; }
    public DateTimeOffset? ConsumedAt { get; private set; }

    public static Result<EmailLoginToken> Create(
        string tokenHash,
        string email,
        string? displayName,
        EmailLoginMode mode,
        string returnUrl,
        DateTimeOffset now,
        TimeSpan lifetime)
    {
        if (tokenHash.Length != TokenHashLength)
            return Result.Failure<EmailLoginToken>(EmailLoginTokenErrors.InvalidToken);

        var normalizedEmail = User.NormalizeEmail(email);
        if (string.IsNullOrEmpty(normalizedEmail) || normalizedEmail.Length > User.MaxEmailLength)
            return Result.Failure<EmailLoginToken>(UserErrors.InvalidEmail);

        var trimmedDisplayName = string.IsNullOrWhiteSpace(displayName) ? null : displayName.Trim();
        if (trimmedDisplayName is not null &&
            trimmedDisplayName.Length is < User.MinDisplayNameLength or > User.MaxDisplayNameLength)
        {
            return Result.Failure<EmailLoginToken>(UserErrors.InvalidDisplayName);
        }

        var safeReturnUrl = string.IsNullOrWhiteSpace(returnUrl) ? "/" : returnUrl.Trim();
        if (safeReturnUrl.Length > MaxReturnUrlLength)
            return Result.Failure<EmailLoginToken>(EmailLoginTokenErrors.InvalidReturnUrl);

        return Result.Success(new EmailLoginToken(
            Guid.NewGuid(),
            tokenHash,
            normalizedEmail,
            trimmedDisplayName,
            mode,
            safeReturnUrl,
            now,
            now.Add(lifetime)));
    }

    public Result Consume(DateTimeOffset now)
    {
        if (ConsumedAt is not null)
            return Result.Failure(EmailLoginTokenErrors.AlreadyConsumed);

        if (ExpiresAt <= now)
            return Result.Failure(EmailLoginTokenErrors.Expired);

        ConsumedAt = now;
        return Result.Success();
    }
}

public static class EmailLoginTokenErrors
{
    public static readonly Error InvalidToken = new(
        "EmailLoginToken.InvalidToken",
        "The login link is invalid.");

    public static readonly Error Expired = new(
        "EmailLoginToken.Expired",
        "The login link has expired.");

    public static readonly Error AlreadyConsumed = new(
        "EmailLoginToken.AlreadyConsumed",
        "The login link has already been used.");

    public static readonly Error InvalidReturnUrl = new(
        "EmailLoginToken.InvalidReturnUrl",
        $"Return URL must be at most {EmailLoginToken.MaxReturnUrlLength} characters.");
}
