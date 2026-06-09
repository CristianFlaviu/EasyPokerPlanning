using PokerPlanning.Domain.Common;

namespace PokerPlanning.Domain.Feedback;

// Plain persisted entity (not an aggregate root): feedback is write-once, has no
// behaviour and raises no domain events. Mirrors the EmailLoginToken entity style.
public sealed class Feedback
{
    public const int MaxNameLength = 80;
    public const int MaxEmailLength = 320;
    public const int MaxMessageLength = 4000;

    private Feedback(
        Guid id,
        string? name,
        string? email,
        string message,
        Guid? userId,
        DateTimeOffset createdAt)
    {
        Id = id;
        Name = name;
        Email = email;
        Message = message;
        UserId = userId;
        CreatedAt = createdAt;
    }

    public Guid Id { get; }
    public string? Name { get; }
    public string? Email { get; }
    public string Message { get; }
    public Guid? UserId { get; }
    public DateTimeOffset CreatedAt { get; }

    public static Result<Feedback> Create(
        string? name,
        string? email,
        string message,
        Guid? userId,
        DateTimeOffset now)
    {
        var trimmedMessage = message?.Trim() ?? string.Empty;
        if (trimmedMessage.Length == 0)
            return Result.Failure<Feedback>(FeedbackErrors.EmptyMessage);
        if (trimmedMessage.Length > MaxMessageLength)
            return Result.Failure<Feedback>(FeedbackErrors.MessageTooLong);

        var trimmedName = string.IsNullOrWhiteSpace(name) ? null : name.Trim();
        if (trimmedName is not null && trimmedName.Length > MaxNameLength)
            return Result.Failure<Feedback>(FeedbackErrors.NameTooLong);

        var trimmedEmail = string.IsNullOrWhiteSpace(email) ? null : email.Trim();
        if (trimmedEmail is not null && trimmedEmail.Length > MaxEmailLength)
            return Result.Failure<Feedback>(FeedbackErrors.EmailTooLong);

        return Result.Success(new Feedback(
            Guid.NewGuid(),
            trimmedName,
            trimmedEmail,
            trimmedMessage,
            userId,
            now));
    }
}

public static class FeedbackErrors
{
    public static readonly Error EmptyMessage = new(
        "Feedback.EmptyMessage",
        "Feedback message cannot be empty.");

    public static readonly Error MessageTooLong = new(
        "Feedback.MessageTooLong",
        $"Feedback message must be at most {Feedback.MaxMessageLength} characters.");

    public static readonly Error NameTooLong = new(
        "Feedback.NameTooLong",
        $"Name must be at most {Feedback.MaxNameLength} characters.");

    public static readonly Error EmailTooLong = new(
        "Feedback.EmailTooLong",
        $"Email must be at most {Feedback.MaxEmailLength} characters.");
}
