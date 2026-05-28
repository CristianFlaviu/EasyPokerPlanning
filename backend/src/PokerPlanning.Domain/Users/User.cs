using PokerPlanning.Domain.Common;
using PokerPlanning.Domain.Users.Events;

namespace PokerPlanning.Domain.Users;

public sealed class User : AggregateRoot
{
    public const int MinDisplayNameLength = 1;
    public const int MaxDisplayNameLength = 80;
    public const int MaxEmailLength = 254;
    public const int MaxAvatarUrlLength = 1024;

    private readonly List<ExternalLogin> _logins = [];

    private User(
        UserId id,
        string email,
        string displayName,
        string? avatarUrl,
        DateTimeOffset createdAt,
        DateTimeOffset lastLoginAt)
    {
        Id = id;
        Email = email;
        DisplayName = displayName;
        AvatarUrl = avatarUrl;
        CreatedAt = createdAt;
        LastLoginAt = lastLoginAt;
    }

    public UserId Id { get; }
    public string Email { get; private set; }
    public string DisplayName { get; private set; }
    public string? AvatarUrl { get; private set; }
    public DateTimeOffset CreatedAt { get; }
    public DateTimeOffset LastLoginAt { get; private set; }

    public IReadOnlyList<ExternalLogin> Logins => _logins.AsReadOnly();

    public static Result<User> Create(
        string email,
        string displayName,
        string? avatarUrl,
        ExternalLogin login,
        DateTimeOffset now)
    {
        var normalizedEmail = NormalizeEmail(email);
        if (string.IsNullOrEmpty(normalizedEmail) || normalizedEmail.Length > MaxEmailLength)
            return Result.Failure<User>(UserErrors.InvalidEmail);

        var trimmedName = (displayName ?? string.Empty).Trim();
        if (trimmedName.Length is < MinDisplayNameLength or > MaxDisplayNameLength)
            return Result.Failure<User>(UserErrors.InvalidDisplayName);

        var trimmedAvatar = string.IsNullOrWhiteSpace(avatarUrl) ? null : avatarUrl.Trim();
        if (trimmedAvatar is { Length: > MaxAvatarUrlLength })
            return Result.Failure<User>(UserErrors.InvalidAvatarUrl);

        if (string.IsNullOrWhiteSpace(login.Provider) || string.IsNullOrWhiteSpace(login.Subject))
            return Result.Failure<User>(UserErrors.InvalidExternalLogin);

        var user = new User(
            UserId.New(),
            normalizedEmail,
            trimmedName,
            trimmedAvatar,
            now,
            now);
        user._logins.Add(login);
        user.RaiseDomainEvent(new UserRegisteredEvent(user.Id, user.Email, login.Provider, now));
        return Result.Success(user);
    }

    public void RecordLogin(DateTimeOffset now)
    {
        LastLoginAt = now;
    }

    public Result UpdateProfile(string displayName, string? avatarUrl)
    {
        var trimmedName = (displayName ?? string.Empty).Trim();
        if (trimmedName.Length is < MinDisplayNameLength or > MaxDisplayNameLength)
            return Result.Failure(UserErrors.InvalidDisplayName);

        var trimmedAvatar = string.IsNullOrWhiteSpace(avatarUrl) ? null : avatarUrl.Trim();
        if (trimmedAvatar is { Length: > MaxAvatarUrlLength })
            return Result.Failure(UserErrors.InvalidAvatarUrl);

        DisplayName = trimmedName;
        AvatarUrl = trimmedAvatar;
        return Result.Success();
    }

    private static string NormalizeEmail(string? email) =>
        string.IsNullOrWhiteSpace(email) ? string.Empty : email.Trim().ToLowerInvariant();
}

public static class UserErrors
{
    public static readonly Error InvalidEmail = new(
        "User.InvalidEmail",
        $"Email must be 1-{User.MaxEmailLength} characters.");

    public static readonly Error InvalidDisplayName = new(
        "User.InvalidDisplayName",
        $"Display name must be {User.MinDisplayNameLength}-{User.MaxDisplayNameLength} characters.");

    public static readonly Error InvalidAvatarUrl = new(
        "User.InvalidAvatarUrl",
        $"Avatar URL must be at most {User.MaxAvatarUrlLength} characters.");

    public static readonly Error InvalidExternalLogin = new(
        "User.InvalidExternalLogin",
        "External login provider and subject are required.");

    public static readonly Error NotFound = new(
        "User.NotFound",
        "User not found.");
}
